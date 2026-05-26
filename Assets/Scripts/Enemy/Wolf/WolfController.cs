using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Xyla.Combat;
using Xyla.Core;

[RequireComponent(typeof(NavMeshAgent))]
public class WolfController : MonoBehaviour
{
    [Header("Wander")]
    [SerializeField] private float _wanderRadius = 8f;
    [SerializeField] private float _wanderIntervalMin = 2f;
    [SerializeField] private float _wanderIntervalMax = 5f;

    [Header("Vision")]
    [SerializeField] private float _visionRange = 15f;  // tăng từ 10 lên 15
    [SerializeField] private float _visionAngle = 120f;
    [SerializeField] private LayerMask _playerLayer;
    [SerializeField] private LayerMask _obstacleLayer;

    [Header("Hearing")]
    [SerializeField] private float _hearingRadius = 10f; // bán kính nghe
    [SerializeField] private float _footstepMinSpeed = 2f;  // tốc độ tối thiểu tạo tiếng

    [Header("Chase")]
    [SerializeField] private float _chaseRadius = 20f; // bán kính tối đa đuổi theo
    [SerializeField] private float _chaseSpeed = 6f;
    [SerializeField] private float _walkSpeed = 2f;
    [SerializeField] private float _loseTargetTime = 3f;

    [Header("Attack")]
    [SerializeField] private float _attackRange = 1.8f;
    [SerializeField] private float _attackDamage = 20f;
    [SerializeField] private float _attackCooldown = 1.5f;

    [Header("Howl")]
    [SerializeField] private float _howlChance = 0.4f;
    [SerializeField] private float _howlDuration = 2.5f;

    [Header("Movement Feel")]
    [SerializeField] private float _rotationSpeed = 80f;  // độ/giây
    [SerializeField] private float _acceleration = 10f;
    [SerializeField] private float _angularSpeed = 0f;

    [Header("Death")]
    [SerializeField] private float _despawnDelay = 120f;

    // ── State ─────────────────────────────────────────────
    private enum State { Wandering, Howling, Chasing, Attacking, Returning, Dead }
    private State _state = State.Wandering;

    private NavMeshAgent _agent;
    private WolfAnimatorController _anim;
    private WolfSpawnPoint _spawnPoint;
    private Transform _player;

    private bool _isDead = false;
    private float _lastAttackTime = 0f;
    private float _lostTargetTimer = 0f;
    private Vector3 _homePosition;

    // ── Lifecycle ─────────────────────────────────────────

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _anim = GetComponent<WolfAnimatorController>();
    }

    public void Initialize(WolfSpawnPoint spawnPoint)
    {
        _spawnPoint = spawnPoint;
        _isDead = false;
        _state = State.Wandering;
        _lostTargetTimer = 0f;
        _player = null;
        _homePosition = transform.position;

        _agent.isStopped = false;
        _agent.speed = _walkSpeed;
        _agent.acceleration = _acceleration;
        _agent.angularSpeed = _angularSpeed;
        _agent.autoBraking = true;
        _agent.stoppingDistance = 0.3f;
        _agent.updateRotation = false;

        if (_anim != null) { _anim.SetDead(false); _anim.SetSpeed(0f); _anim.SetHowling(false); }

        var loot = GetComponent<WolfLoot>();
        if (loot != null) loot.SetLootable(false);

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

        if (_anim != null) _anim.SetSpeed(_agent.velocity.magnitude);

        if (_state == State.Chasing) UpdateChase();
        if (_state == State.Returning) UpdateReturn();
    }

    private void SmoothRotation()
    {
        // Khi attack — quay nhanh hơn về phía player
        if (_state == State.Attacking)
        {
            if (_player != null)
            {
                Vector3 dir = _player.position - transform.position;
                dir.y = 0;
                if (dir != Vector3.zero)
                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation,
                        Quaternion.LookRotation(dir),
                        200f * Time.deltaTime
                    );
            }
            return;
        }

        if (_agent.velocity.sqrMagnitude < 0.1f) return;

        Vector3 moveDir = _agent.velocity.normalized;
        moveDir.y = 0;
        if (moveDir == Vector3.zero) return;

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            Quaternion.LookRotation(moveDir),
            _rotationSpeed * Time.deltaTime
        );
    }

    // ── Wander ────────────────────────────────────────────

    private IEnumerator WanderRoutine()
    {
        while (!_isDead)
        {
            if (_state == State.Wandering)
            {
                _homePosition = transform.position;
                Vector3 target = GetRandomNavMeshPoint(transform.position, _wanderRadius);
                _agent.SetDestination(target);
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
            if (_state != State.Wandering && _state != State.Howling) continue;

            // Hearing không cần nhìn thấy — đi sau lưng vẫn bị nghe
            bool heard = DetectByHearing();

            // Vision chỉ phát hiện phía trước
            bool seen = !heard && DetectByVision();

            if (heard || seen)
                StartCoroutine(AlertRoutine());
        }
    }

    private bool DetectByVision()
    {
        // Dùng Physics.OverlapSphere không có LayerMask để debug
        // nếu _playerLayer chưa set đúng
        Collider[] hits = _playerLayer == 0
            ? Physics.OverlapSphere(transform.position, _visionRange)
            : Physics.OverlapSphere(transform.position, _visionRange, _playerLayer);

        foreach (var hit in hits)
        {
            // Tìm object có tag Player
            if (!hit.CompareTag("Player")) continue;

            Transform target = hit.transform;
            Vector3 dirToTarget = (target.position - transform.position).normalized;

            // Kiểm tra góc nhìn
            float angle = Vector3.Angle(transform.forward, dirToTarget);
            if (angle > _visionAngle * 0.5f) continue;

            // Raycast kiểm tra vật cản
            float dist = Vector3.Distance(transform.position, target.position);
            Ray ray = new Ray(transform.position + Vector3.up * 0.5f, dirToTarget);
            if (Physics.Raycast(ray, dist, _obstacleLayer)) continue;

            _player = target;
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

            Transform target = hit.transform;
            float playerSpeed = 0f;

            var rb = target.GetComponent<Rigidbody>();
            if (rb != null)
                playerSpeed = rb.linearVelocity.magnitude;
            else
            {
                var cc = target.GetComponent<CharacterController>();
                if (cc != null) playerSpeed = cc.velocity.magnitude;
            }

            if (playerSpeed < _footstepMinSpeed) continue;

            _player = target;
            return true;
        }
        return false;
    }

    // ── Alert ─────────────────────────────────────────────

    private IEnumerator AlertRoutine()
    {
        if (_state != State.Wandering) yield break;
        _agent.ResetPath();

        if (Random.value < _howlChance)
        {
            _state = State.Howling;
            if (_anim != null) _anim.SetHowling(true);
            yield return new WaitForSeconds(_howlDuration);
            if (_anim != null) _anim.SetHowling(false);
        }

        if (!_isDead) BeginChase();
    }

    // ── Chase ─────────────────────────────────────────────

    private void BeginChase()
    {
        _homePosition = transform.position;
        _state = State.Chasing;
        _agent.speed = _chaseSpeed;
        _lostTargetTimer = 0f;
    }

    private void UpdateChase()
    {
        if (_player == null) { BeginReturn(); return; }

        float dist = Vector3.Distance(transform.position, _player.position);

        // Vượt quá bán kính đuổi tối đa → bỏ cuộc
        if (dist > _chaseRadius)
        {
            BeginReturn();
            return;
        }

        // Đủ gần → tấn công
        if (dist <= _attackRange)
        {
            TryAttack();
            return;
        }

        _agent.SetDestination(_player.position);

        // Mất tầm nhìn → đếm timer
        bool canDetect = DetectByVision() || DetectByHearing();
        if (!canDetect)
        {
            _lostTargetTimer += Time.deltaTime;
            if (_lostTargetTimer >= _loseTargetTime)
                BeginReturn();
        }
        else
        {
            _lostTargetTimer = 0f;
        }
    }

    // ── Return ────────────────────────────────────────────

    private void BeginReturn()
    {
        _state = State.Returning;
        _agent.speed = _walkSpeed;
        _player = null;
        _lostTargetTimer = 0f;
        _agent.SetDestination(_homePosition);
    }

    private void UpdateReturn()
    {
        if (!_agent.pathPending && _agent.remainingDistance < 0.5f)
        {
            _state = State.Wandering;
            _homePosition = transform.position;
        }

        if (DetectByVision() || DetectByHearing())
            BeginChase();
    }

    // ── Attack ────────────────────────────────────────────

    private void TryAttack()
    {
        if (Time.time < _lastAttackTime + _attackCooldown) return;
        if (_state == State.Dead) return;

        _lastAttackTime = Time.time;
        _state = State.Attacking;
        _agent.ResetPath();

        if (_anim != null) _anim.TriggerAttack();
        StartCoroutine(ApplyAttackDamage());
    }

    private IEnumerator ApplyAttackDamage()
    {
        yield return new WaitForSeconds(0.4f);

        if (_isDead || _player == null) yield break;

        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist <= _attackRange + 0.5f)
        {
            var damageable = _player.GetComponentInParent<IDamageable>();
            if (damageable != null && !damageable.IsDead)
                damageable.TakeDamage(_attackDamage, gameObject);
        }

        yield return new WaitForSeconds(0.5f);
        if (!_isDead) BeginChase();
    }

    public void OnTakeDamage(Transform attacker)
    {
        if (_isDead) return;
        _player = attacker;
        if (_state != State.Chasing && _state != State.Attacking)
            BeginChase();
    }

    // ── Death ─────────────────────────────────────────────

    public void Die()
    {
        if (_isDead) return;
        _isDead = true;
        _state = State.Dead;

        StopAllCoroutines();
        _agent.isStopped = true;
        _agent.ResetPath();

        if (_anim != null) { _anim.SetSpeed(0f); _anim.SetDead(true); _anim.SetHowling(false); }

        var loot = GetComponent<WolfLoot>();
        if (loot != null) loot.SetLootable(true);

        ObjectPool.Instance.ReturnDelayed(gameObject, _despawnDelay);

        if (_spawnPoint != null)
            _spawnPoint.Invoke("OnWolfDespawned", _despawnDelay);
    }

    private void OnDrawGizmosSelected()
    {
        // Vùng nghe
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _hearingRadius);

        // Vùng nhìn
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _visionRange);

        // Bán kính đuổi tối đa
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, _chaseRadius);

        // Góc nhìn
        Gizmos.color = Color.cyan;
        float half = _visionAngle * 0.5f;
        Gizmos.DrawRay(transform.position,
            Quaternion.Euler(0, -half, 0) * transform.forward * _visionRange);
        Gizmos.DrawRay(transform.position,
            Quaternion.Euler(0, half, 0) * transform.forward * _visionRange);

        // Home
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(_homePosition, 0.5f);
    }
}