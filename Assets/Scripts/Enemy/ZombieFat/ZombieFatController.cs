using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Xyla.Combat;
using Xyla.Core;
using Xyla.Player;

/// <summary>
/// AI ZombieFat:
/// - Wander ngẫu nhiên khi không phát hiện player
/// - Chỉ dùng Run khi đuổi theo player (không có Walk chase)
/// - Combo claw: Attack_Claw_1 → Attack_Claw_2 (tay trái rồi tay phải)
/// - Sau 10 giây tấn công: dùng Attack_Special (phun axit từ miệng)
/// - Sneak player: không bị phát hiện qua nghe tiếng nếu noise thấp
/// - Chết: ragdoll + rơi bộ phận ngẫu nhiên + despawn sau 2 phút qua ObjectPool
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class ZombieFatController : MonoBehaviour
{
    [Header("Wander")]
    [SerializeField] private float _wanderRadius = 4f;
    [SerializeField] private float _wanderIntervalMin = 2f;
    [SerializeField] private float _wanderIntervalMax = 5f;
    [SerializeField] private float _wanderSpeed = 1.5f;

    [Header("Detection — Vision")]
    [SerializeField] private float _visionRange = 10f;
    [SerializeField] private float _visionAngle = 100f;
    [SerializeField] private LayerMask _playerLayer;
    [SerializeField] private LayerMask _obstacleLayer;

    [Header("Detection — Hearing")]
    [SerializeField] private float _hearingRadius = 7f;
    [Tooltip("Mức noise tối thiểu để phát hiện. NoiseSneakWalk=0.15 nên đặt > 0.15.")]
    [SerializeField] private float _hearingNoiseThreshold = 0.3f;

    [Header("Chase — Run Only")]
    [SerializeField] private float _chaseSpeed = 4.5f;
    [SerializeField] private float _chaseRadius = 25f;
    [SerializeField] private float _loseTargetTime = 4f;

    [Header("Attack — Claw Combo")]
    [Tooltip("Tầm tấn công claw.")]
    [SerializeField] private float _attackRange = 2f;
    [SerializeField] private float _attackDamage = 20f;
    [Tooltip("Cooldown giữa các combo claw (giây).")]
    [SerializeField] private float _attackCooldown = 2f;

    [Header("Attack — Special (Acid)")]
    [Tooltip("Sau bao nhiêu giây kể từ lần tấn công đầu thì dùng skill axit.")]
    [SerializeField] private float _specialCooldown = 10f;
    [Tooltip("Tầm bắn axit — thường xa hơn tầm claw.")]
    [SerializeField] private float _specialRange = 8f;
    [SerializeField] private float _specialDamage = 30f;

    [Header("Special Effect")]
    [Tooltip("Prefab effect axit bắn từ miệng. Cần có Rigidbody + Collider(IsTrigger).")]
    [SerializeField] private GameObject _acidEffectPrefab;
    [Tooltip("Vị trí miệng zombie — kéo bone đầu hoặc empty object tại miệng.")]
    [SerializeField] private Transform _mouthTransform;
    [SerializeField] private float _acidSpeed = 7f;
    [SerializeField] private float _acidLifetime = 3f;

    [Header("Death")]
    [SerializeField] private float _despawnDelay = 120f;

    // ── State ──────────────────────────────────────────────

    private enum State { Wandering, Chasing, Dead }
    private State _state = State.Wandering;

    private NavMeshAgent _agent;
    private ZombieFatAnimatorController _anim;
    private ZombieFatSpawnPoint _spawnPoint;
    private Transform _player;
    private NoiseEmitter _playerNoise;

    private bool _isDead = false;
    private bool _isAttacking = false;
    private float _lastAttackTime = -999f;
    private float _lastClawTime = -999f; // tính 10s special từ lần claw xong
    private float _lostTargetTimer = 0f;

    // ── Lifecycle ──────────────────────────────────────────

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _anim = GetComponent<ZombieFatAnimatorController>();
    }

    public void Initialize(ZombieFatSpawnPoint spawnPoint)
    {
        _spawnPoint = spawnPoint;
        _isDead = false;
        _isAttacking = false;
        _state = State.Wandering;
        _lostTargetTimer = 0f;
        _player = null;
        _playerNoise = null;
        _lastAttackTime = -999f;
        _lastClawTime = -999f;

        _agent.isStopped = false;
        _agent.speed = _wanderSpeed;
        _agent.autoBraking = true;
        _agent.stoppingDistance = 0.1f;
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

        // Force dừng khi đang tấn công — không trượt
        if (_isAttacking)
        {
            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
        }

        SmoothRotation();

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

    // ── Wander ────────────────────────────────────────────

    private IEnumerator WanderRoutine()
    {
        while (!_isDead)
        {
            if (_state == State.Wandering)
            {
                Vector3 target = GetRandomNavMeshPoint(transform.position, _wanderRadius);
                _agent.SetDestination(target);
                if (_anim != null) _anim.SetWalking(true); // wander dùng walk

                float timeout = 8f, elapsed = 0f;
                while (_state == State.Wandering && elapsed < timeout)
                {
                    elapsed += Time.deltaTime;
                    if (!_agent.pathPending && _agent.remainingDistance < 0.3f) break;
                    yield return null;
                }

                if (_anim != null) _anim.SetIdle(); // đến nơi → về idle
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
            if (detected) BeginChase();
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
            // Sneak (noise 0.15) < threshold (0.3) → không phát hiện
            if (noise == null || !noise.IsActive) continue;
            if (noise.LastNoiseLevel < _hearingNoiseThreshold) continue;
            _player = hit.transform;
            _playerNoise = noise;
            return true;
        }
        return false;
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

        // Kiểm tra special trước — ưu tiên hơn claw sau 10s kể từ lần claw cuối
        // dist <= _specialRange đủ để dùng (bao gồm cả khi player trong tầm claw)
        bool specialReady = !_isAttacking
                         && _lastClawTime > -999f
                         && Time.time >= _lastClawTime + _specialCooldown
                         && dist <= _specialRange;

        if (specialReady)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
            _agent.velocity = Vector3.zero;
            if (_anim != null) _anim.SetIdle();
            StartCoroutine(PerformSpecialAttack());
            return;
        }

        // Trong tầm claw và special chưa sẵn sàng → claw combo
        if (dist <= _attackRange)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
            _agent.velocity = Vector3.zero;
            if (_anim != null) _anim.SetIdle();
            TryClawAttack();
            return;
        }

        // Ngoài tầm → chạy đuổi theo (Run animation)
        _agent.isStopped = false;
        _agent.SetDestination(_player.position);
        if (_anim != null) _anim.SetRunning(true);

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
        if (_anim != null) _anim.SetIdle();
    }

    // ── Claw Attack (Combo) ────────────────────────────────

    private void TryClawAttack()
    {
        if (_isAttacking) return;
        if (Time.time < _lastAttackTime + _attackCooldown) return;
        if (_isDead) return;

        _isAttacking = true;
        _lastAttackTime = Time.time;

        if (_anim != null) _anim.TriggerAttackClaw();
        StartCoroutine(ClawComboRoutine());
    }

    private IEnumerator ClawComboRoutine()
    {
        // Claw 1 — tay trái hit
        yield return new WaitForSeconds(0.4f);
        ApplyClawDamage();

        // Chờ animation chuyển sang Claw 2
        yield return new WaitForSeconds(0.5f);

        // Claw 2 — tay phải hit
        yield return new WaitForSeconds(0.35f);
        ApplyClawDamage();

        // Chờ animation kết thúc
        yield return new WaitForSeconds(0.5f);
        _isAttacking = false;

        // Ghi lại thời điểm claw xong — special sẽ tính 10s từ đây
        _lastClawTime = Time.time;
    }

    private void ApplyClawDamage()
    {
        if (_isDead || _player == null) return;
        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist > _attackRange + 0.5f) return;

        var damageable = _player.GetComponentInParent<IDamageable>();
        if (damageable != null && !damageable.IsDead)
            damageable.TakeDamage(_attackDamage, gameObject);
    }

    // ── Special Attack (Acid) ──────────────────────────────

    private IEnumerator PerformSpecialAttack()
    {
        _isAttacking = true;

        if (_anim != null) _anim.TriggerSpecialAttack();

        // Chờ đến frame bắn axit trong animation
        yield return new WaitForSeconds(0.6f);

        if (!_isDead && _player != null)
            FireAcid();

        // Chờ animation kết thúc
        yield return new WaitForSeconds(1f);
        _isAttacking = false;

        // Reset lại timer claw để 10s tiếp theo mới special lại
        _lastClawTime = Time.time;
    }

    private void FireAcid()
    {
        if (_acidEffectPrefab == null)
        {
            Debug.LogWarning("[ZombieFat] Chưa gán _acidEffectPrefab!", this);
            return;
        }

        Vector3 spawnPos = _mouthTransform != null
            ? _mouthTransform.position
            : transform.position + Vector3.up * 1.8f;

        GameObject proj = ObjectPool.Instance.Get(_acidEffectPrefab, spawnPos, Quaternion.identity);
        if (proj == null) return;

        var projectile = proj.GetComponent<ZombieFatAcid>();
        if (projectile != null)
            projectile.Initialize(_player, _specialDamage, _acidSpeed, _acidLifetime, gameObject);
    }

    // ── Bị tấn công bởi Player ────────────────────────────

    public void OnTakeDamage(Transform attacker)
    {
        if (_isDead) return;
        _player = attacker;
        _playerNoise = attacker.GetComponent<NoiseEmitter>();
        if (_state != State.Chasing) BeginChase();
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

        if (_anim != null) { _anim.SetIdle(); _anim.TriggerDeath(); }

        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        ObjectPool.Instance.ReturnDelayed(gameObject, _despawnDelay);
        if (_spawnPoint != null)
            _spawnPoint.Invoke("OnZombieFatDespawned", _despawnDelay);
    }

    // ── Gizmos ────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _hearingRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _visionRange);
        Gizmos.color = new Color(0.5f, 0f, 1f);
        Gizmos.DrawWireSphere(transform.position, _specialRange);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, _chaseRadius);
        float half = _visionAngle * 0.5f;
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position,
            Quaternion.Euler(0, -half, 0) * transform.forward * _visionRange);
        Gizmos.DrawRay(transform.position,
            Quaternion.Euler(0, half, 0) * transform.forward * _visionRange);
    }
}