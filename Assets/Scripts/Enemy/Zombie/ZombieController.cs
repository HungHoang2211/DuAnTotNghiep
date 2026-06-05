using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Xyla.Combat;
using Xyla.Core;
using Xyla.Player;

/// <summary>
/// AI Zombie dùng chung cho cả zombie Walk và zombie Run.
/// Chọn loại di chuyển bằng field _moveType trong Inspector:
///   - WalkOnly : chỉ dùng animation walk khi đuổi theo player
///   - RunOnly  : chỉ dùng animation run khi đuổi theo player
/// Khi player trong tầm tấn công: đứng yên tấn công, không trượt.
/// Khi player ra ngoài tầm: đuổi theo bằng animation đã chọn.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class ZombieController : MonoBehaviour
{
    // ── Move Type ──────────────────────────────────────────

    public enum MoveType { WalkOnly, RunOnly }

    [Header("Move Type")]
    [Tooltip("WalkOnly = chỉ dùng walk khi đuổi.\nRunOnly = chỉ dùng run khi đuổi.")]
    [SerializeField] private MoveType _moveType = MoveType.WalkOnly;

    // ── Inspector ──────────────────────────────────────────

    [Header("Wander")]
    [SerializeField] private float _wanderRadius = 4f;
    [SerializeField] private float _wanderIntervalMin = 2f;
    [SerializeField] private float _wanderIntervalMax = 5f;

    [Header("Detection — Vision")]
    [SerializeField] private float _visionRange = 12f;
    [SerializeField] private float _visionAngle = 100f;
    [SerializeField] private LayerMask _playerLayer;
    [SerializeField] private LayerMask _obstacleLayer;

    [Header("Detection — Hearing")]
    [SerializeField] private float _hearingRadius = 8f;
    [SerializeField] private float _hearingNoiseThreshold = 0.3f;

    [Header("Chase Speed")]
    [Tooltip("Tốc độ khi wander (luôn dùng walk animation).")]
    [SerializeField] private float _wanderSpeed = 1.5f;
    [Tooltip("Tốc độ khi đuổi player (khớp với animation đã chọn).")]
    [SerializeField] private float _chaseSpeed = 4f;
    [SerializeField] private float _chaseRadius = 25f;
    [SerializeField] private float _loseTargetTime = 4f;

    [Header("Attack")]
    [SerializeField] private float _attackRange = 1.6f;
    [SerializeField] private float _attackDamage = 15f;
    [SerializeField] private float _attackCooldown = 1.8f;

    [Header("Howl")]
    [SerializeField][Range(0f, 1f)] private float _howlChance = 0.7f;
    [SerializeField] private float _howlDuration = 2f;

    [Header("Death")]
    [SerializeField] private float _despawnDelay = 120f;

    // ── State ──────────────────────────────────────────────

    private enum State { Wandering, Alerting, Chasing, Dead }
    private State _state = State.Wandering;

    private NavMeshAgent _agent;
    private ZombieAnimatorController _anim;
    private ZombieSpawnPoint _spawnPoint;
    private Transform _player;
    private NoiseEmitter _playerNoise;

    private bool _isDead = false;
    private bool _isAttacking = false;
    private float _lastAttackTime = -999f;
    private float _lostTargetTimer = 0f;
    private Vector3 _homePosition;

    // ── Lifecycle ──────────────────────────────────────────

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _anim = GetComponent<ZombieAnimatorController>();
    }

    public void Initialize(ZombieSpawnPoint spawnPoint)
    {
        _spawnPoint = spawnPoint;
        _isDead = false;
        _isAttacking = false;
        _state = State.Wandering;
        _lostTargetTimer = 0f;
        _player = null;
        _playerNoise = null;
        _homePosition = transform.position;
        _lastAttackTime = -999f;

        _agent.isStopped = false;
        _agent.speed = _wanderSpeed;
        _agent.autoBraking = true;
        _agent.stoppingDistance = 0.1f;
        _agent.angularSpeed = 0f;
        _agent.updateRotation = false;

        if (_anim != null) _anim.ResetForSpawn();

        var col = GetComponent<Collider>();
        if (col != null) col.enabled = true;

        StopAllCoroutines();
        StartCoroutine(WanderRoutine());
        StartCoroutine(DetectionRoutine());
    }

    private void OnSpawnFromPool() { }

    private void Update()
    {
        if (_isDead) return;
        SmoothRotation();

        // Force dừng hoàn toàn khi đang tấn công
        if (_isAttacking)
        {
            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
        }

        if (_state == State.Chasing) UpdateChase();
    }

    private void SmoothRotation()
    {
        if (_isAttacking && _player != null)
        {
            Vector3 dir = _player.position - transform.position;
            dir.y = 0;
            if (dir != Vector3.zero)
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, Quaternion.LookRotation(dir), 300f * Time.deltaTime);
            return;
        }

        if (_agent.velocity.sqrMagnitude < 0.05f) return;
        Vector3 moveDir = _agent.velocity.normalized;
        moveDir.y = 0;
        if (moveDir == Vector3.zero) return;
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, Quaternion.LookRotation(moveDir), 120f * Time.deltaTime);
    }

    // ── Animation Helper ───────────────────────────────────

    /// <summary>
    /// Bật animation di chuyển theo MoveType đã chọn.
    /// WalkOnly → SetWalking(true) | RunOnly → SetRunning(true)
    /// </summary>
    private void PlayChaseAnimation()
    {
        if (_anim == null) return;
        if (_moveType == MoveType.WalkOnly)
            _anim.SetWalking(true);
        else
            _anim.SetRunning(true);
    }

    // ── Wander ────────────────────────────────────────────

    private IEnumerator WanderRoutine()
    {
        while (!_isDead)
        {
            if (_state == State.Wandering)
            {
                Vector3 target = GetRandomNavMeshPoint(transform.position, _wanderRadius);
                _agent.SetDestination(target);
                if (_anim != null) _anim.SetWalking(true); // wander luôn dùng walk

                float timeout = 8f, elapsed = 0f;
                while (_state == State.Wandering && elapsed < timeout)
                {
                    elapsed += Time.deltaTime;
                    if (!_agent.pathPending && _agent.remainingDistance < 0.3f) break;
                    yield return null;
                }

                if (_anim != null) _anim.SetIdle();
            }
            yield return new WaitForSeconds(Random.Range(_wanderIntervalMin, _wanderIntervalMax));
        }
    }

    private Vector3 GetRandomNavMeshPoint(Vector3 origin, float radius)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector2 c = Random.insideUnitCircle * radius;
            Vector3 candidate = origin + new Vector3(c.x, 0, c.y);
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, radius, NavMesh.AllAreas))
                return hit.position;
        }
        return origin;
    }

    // ── Detection ─────────────────────────────────────────

    private IEnumerator DetectionRoutine()
    {
        while (!_isDead)
        {
            yield return new WaitForSeconds(0.2f);
            if (_state == State.Dead) yield break;
            if (_state != State.Wandering) continue;

            bool detected = DetectByVision() || DetectByHearing();
            if (detected) StartCoroutine(AlertRoutine());
        }
    }

    private bool DetectByVision()
    {
        Collider[] hits = _playerLayer == 0
            ? Physics.OverlapSphere(transform.position, _visionRange)
            : Physics.OverlapSphere(transform.position, _visionRange, _playerLayer);

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;
            Transform target = hit.transform;
            Vector3 dirToTarget = (target.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, dirToTarget);
            if (angle > _visionAngle * 0.5f) continue;
            float dist = Vector3.Distance(transform.position, target.position);
            Ray ray = new Ray(transform.position + Vector3.up * 0.8f, dirToTarget);
            if (_obstacleLayer != 0 && Physics.Raycast(ray, dist, _obstacleLayer)) continue;
            _player = target;
            _playerNoise = target.GetComponent<NoiseEmitter>();
            return true;
        }
        return false;
    }

    private bool DetectByHearing()
    {
        Collider[] hits = _playerLayer == 0
            ? Physics.OverlapSphere(transform.position, _hearingRadius)
            : Physics.OverlapSphere(transform.position, _hearingRadius, _playerLayer);

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;
            var noise = hit.GetComponent<NoiseEmitter>();
            if (noise == null || !noise.IsActive) continue;
            if (noise.LastNoiseLevel < _hearingNoiseThreshold) continue;
            _player = hit.transform;
            _playerNoise = noise;
            return true;
        }
        return false;
    }

    // ── Alert (Hú trước khi đuổi) ─────────────────────────

    private IEnumerator AlertRoutine()
    {
        if (_state != State.Wandering) yield break;
        _state = State.Alerting;

        _agent.isStopped = true;
        _agent.ResetPath();
        if (_anim != null) _anim.SetIdle();

        if (_player != null)
        {
            Vector3 dir = (_player.position - transform.position); dir.y = 0;
            if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir);
        }

        if (Random.value <= _howlChance)
        {
            if (_anim != null) _anim.SetHowling(true);
            yield return new WaitForSeconds(_howlDuration);
            if (_anim != null) _anim.SetHowling(false);
        }

        if (!_isDead) BeginChase();
    }

    // ── Chase ─────────────────────────────────────────────

    private void BeginChase()
    {
        _state = State.Chasing;
        _agent.isStopped = false;
        _agent.speed = _chaseSpeed;
        _lostTargetTimer = 0f;
    }

    private void UpdateChase()
    {
        if (_player == null) { BeginWander(); return; }

        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist > _chaseRadius) { BeginWander(); return; }

        // Trong tầm attack → đứng yên tấn công
        if (dist <= _attackRange)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
            _agent.velocity = Vector3.zero;
            if (_anim != null) _anim.SetIdle();
            TryAttack();
            return;
        }

        // Ngoài tầm → đuổi theo bằng animation đã chọn
        _agent.isStopped = false;
        _agent.SetDestination(_player.position);
        PlayChaseAnimation();

        bool canDetect = CanStillDetect();
        if (!canDetect)
        {
            _lostTargetTimer += Time.deltaTime;
            if (_lostTargetTimer >= _loseTargetTime) BeginWander();
        }
        else _lostTargetTimer = 0f;
    }

    private bool CanStillDetect()
    {
        if (_player == null) return false;
        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist <= _visionRange)
        {
            Vector3 dir = (_player.position - transform.position).normalized;
            Ray ray = new Ray(transform.position + Vector3.up * 0.8f, dir);
            if (_obstacleLayer == 0 || !Physics.Raycast(ray, dist, _obstacleLayer)) return true;
        }
        if (_playerNoise != null && _playerNoise.IsActive
            && _playerNoise.LastNoiseLevel >= _hearingNoiseThreshold) return true;
        return false;
    }

    private void BeginWander()
    {
        _state = State.Wandering;
        _agent.isStopped = false;
        _agent.speed = _wanderSpeed;
        _player = null;
        _playerNoise = null;
        _lostTargetTimer = 0f;
        if (_anim != null) { _anim.SetHowling(false); _anim.SetIdle(); }
    }

    // ── Attack ────────────────────────────────────────────

    private void TryAttack()
    {
        if (_isAttacking) return;
        if (Time.time < _lastAttackTime + _attackCooldown) return;
        if (_isDead) return;

        _isAttacking = true;
        _lastAttackTime = Time.time;

        if (_anim != null) _anim.TriggerAttack();
        StartCoroutine(ApplyDamage());
    }

    private IEnumerator ApplyDamage()
    {
        yield return new WaitForSeconds(0.4f);

        if (!_isDead && _player != null)
        {
            float dist = Vector3.Distance(transform.position, _player.position);
            if (dist <= _attackRange + 0.5f)
            {
                var damageable = _player.GetComponentInParent<IDamageable>();
                if (damageable != null && !damageable.IsDead)
                    damageable.TakeDamage(_attackDamage, gameObject);
            }
        }

        yield return new WaitForSeconds(0.8f);
        _isAttacking = false;
    }

    // ── Được tấn công bởi Player ──────────────────────────

    public void OnTakeDamage(Transform attacker)
    {
        if (_isDead) return;
        _player = attacker;
        _playerNoise = attacker.GetComponent<NoiseEmitter>();
        if (_state == State.Wandering) StartCoroutine(AlertRoutine());
        else if (_state == State.Chasing) BeginChase();
    }

    // ── Death ─────────────────────────────────────────────

    public void Die()
    {
        if (_isDead) return;
        _isDead = true;
        _isAttacking = false;
        _state = State.Dead;

        StopAllCoroutines();
        _agent.isStopped = true;
        _agent.ResetPath();

        if (_anim != null) { _anim.SetHowling(false); _anim.SetIdle(); _anim.TriggerDeath(); }

        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        ObjectPool.Instance.ReturnDelayed(gameObject, _despawnDelay);
        if (_spawnPoint != null) _spawnPoint.Invoke("OnZombieDespawned", _despawnDelay);
    }

    // ── Gizmos ────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _hearingRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _visionRange);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, _chaseRadius);
        Gizmos.color = Color.cyan;
        float half = _visionAngle * 0.5f;
        Gizmos.DrawRay(transform.position,
            Quaternion.Euler(0, -half, 0) * transform.forward * _visionRange);
        Gizmos.DrawRay(transform.position,
            Quaternion.Euler(0, half, 0) * transform.forward * _visionRange);
    }
}