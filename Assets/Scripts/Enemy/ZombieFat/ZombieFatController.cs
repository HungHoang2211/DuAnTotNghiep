using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using SimpleSurvival.Combat;
using SimpleSurvival.Input;
using SimpleSurvival.Stats;
using SimpleSurvival.Core;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyStats))]
public class ZombieFatController : MonoBehaviour
{
    [Header("Detection Layers")]
    [SerializeField] private LayerMask _playerLayer;
    [SerializeField] private LayerMask _obstacleLayer;

    [Header("Special Effect References")]
    [Tooltip("Prefab effect axit bắn từ miệng. Cần có Rigidbody + Collider(IsTrigger).")]
    [SerializeField] private GameObject _acidEffectPrefab;
    [Tooltip("Vị trí miệng zombie — kéo bone đầu hoặc empty object tại miệng.")]
    [SerializeField] private Transform _mouthTransform;

    [Header("Jump Attack")]
    [Tooltip("Prefab effect xuất hiện khi chân chạm đất (gắn qua Animation Event).")]
    [SerializeField] private GameObject _jumpImpactEffectPrefab;
    [Tooltip("Sau bao nhiêu giây liên tục đứng trong tầm _isAttacking thì dùng JumpAttack.")]
    [SerializeField] private float _jumpAttackDelay = 5f;
    [Tooltip("Bán kính vùng damage khi giậm chân.")]
    [SerializeField] private float _jumpLandRadius = 1.5f;
    [Tooltip("Damage của JumpAttack.")]
    [SerializeField] private float _jumpDamage = 35f;
    [Tooltip("Thời gian choáng áp lên player khi JumpAttack trúng.")]
    [SerializeField] private float _jumpStunDuration = 2f;

    private enum State { Wandering, Chasing, Dead }
    private State _state = State.Wandering;

    private NavMeshAgent _agent;
    private ZombieFatAnimatorController _anim;
    private ZombieFatSpawnPoint _spawnPoint;
    private EnemyStats _stats;
    private Transform _player;

    private bool _isDead = false;
    private bool _isAttacking = false;
    private bool _attackCancelled = false;   // flag cancel khi player thoát tầm giữa chừng
    private float _lastAttackTime = -999f;
    private float _lastClawTime = -999f;
    private float _lostTargetTimer = 0f;
    private PlayerInputReader _playerInputReader;

    // Jump Attack
    private float _firstClawTime = -999f;    // thời điểm lần đầu dùng claw trong combat
    private bool _jumpAttackReady = false;   // true khi đủ 5 giây từ claw đầu tiên
    private bool _isJumpAttacking = false;   // đang thực hiện JumpAttack

    private ZombieFatStatsConfig Config => _stats != null ? _stats.EnemyConfig as ZombieFatStatsConfig : null;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _anim = GetComponent<ZombieFatAnimatorController>();
        _stats = GetComponent<EnemyStats>();

        if (_stats == null)
        {
            Debug.LogError($"[{name}] Missing EnemyStats component", this);
            return;
        }

        _stats.OnDeath += HandleDeath;
        _stats.OnDamagedBy += HandleDamagedBy;

        if (_anim != null)
            _anim.OnJumpAttackImpact += HandleJumpAttackImpact;
    }

    private void OnDestroy()
    {
        if (_stats != null)
        {
            _stats.OnDeath -= HandleDeath;
            _stats.OnDamagedBy -= HandleDamagedBy;
        }

        if (_anim != null)
            _anim.OnJumpAttackImpact -= HandleJumpAttackImpact;
    }

    public void Initialize(ZombieFatSpawnPoint spawnPoint)
    {
        if (Config == null)
        {
            Debug.LogError($"[{name}] ZombieFatStatsConfig missing on EnemyStats", this);
            return;
        }

        _spawnPoint = spawnPoint;
        _isDead = false;
        _isAttacking = false;
        _attackCancelled = false;
        _state = State.Wandering;
        _lostTargetTimer = 0f;
        _player = null;
        _playerInputReader = null;
        _lastAttackTime = -999f;
        _lastClawTime = -999f;
        _firstClawTime = -999f;
        _jumpAttackReady = false;
        _isJumpAttacking = false;

        _agent.isStopped = false;
        _agent.speed = Config.WanderSpeed;
        _agent.autoBraking = true;
        _agent.stoppingDistance = 0.1f;
        _agent.angularSpeed = 360f;
        _agent.acceleration = 16f;
        _agent.updateRotation = true;

        if (_anim != null) _anim.ResetForSpawn();

        var col = GetComponent<Collider>();
        if (col != null) col.enabled = true;

        StopAllCoroutines();
        StartCoroutine(WanderRoutine());
        StartCoroutine(DetectionRoutine());
    }

    private void HandleDeath() => Die();

    private void HandleDamagedBy(GameObject source)
    {
        if (source != null)
            OnTakeDamage(source.transform);
    }

    private void Update()
    {
        if (_isDead) return;

        if (_isAttacking)
        {
            // Khoá hoàn toàn agent khi đang attack — không cho UpdateChase chen vào
            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
            SmoothRotation();
            return;  // ← thoát sớm, không gọi UpdateChase
        }

        SmoothRotation();

        if (_state == State.Chasing) UpdateChase();
    }

    private void SmoothRotation()
    {
        if (_isAttacking && _player != null)
        {
            _agent.updateRotation = false;
            Vector3 dir = _player.position - transform.position;
            dir.y = 0;
            if (dir != Vector3.zero)
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, Quaternion.LookRotation(dir), 360f * Time.deltaTime);
            return;
        }

        _agent.updateRotation = true;
    }

    private IEnumerator WanderRoutine()
    {
        while (!_isDead)
        {
            if (Config == null) yield break;

            if (_state == State.Wandering)
            {
                Vector3 target = GetRandomNavMeshPoint(transform.position, Config.WanderRadius);
                _agent.SetDestination(target);
                if (_anim != null) _anim.SetWalking(true);

                float timeout = 8f, elapsed = 0f;
                while (_state == State.Wandering && elapsed < timeout)
                {
                    elapsed += Time.deltaTime;
                    if (!_agent.pathPending && _agent.remainingDistance < 0.3f) break;
                    yield return null;
                }

                if (_anim != null) _anim.SetIdle();
            }
            yield return new WaitForSeconds(Random.Range(Config.WanderIntervalMin, Config.WanderIntervalMax));
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
        if (Config == null) return false;

        Collider[] hits = _playerLayer == 0
            ? Physics.OverlapSphere(transform.position, Config.VisionRange)
            : Physics.OverlapSphere(transform.position, Config.VisionRange, _playerLayer);

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;
            Transform target = hit.transform;
            Vector3 dirToTarget = (target.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, dirToTarget);
            if (angle > Config.VisionAngle * 0.5f) continue;
            float dist = Vector3.Distance(transform.position, target.position);
            Ray ray = new Ray(transform.position + Vector3.up * 0.8f, dirToTarget);
            if (_obstacleLayer != 0 && Physics.Raycast(ray, dist, _obstacleLayer)) continue;
            _player = target;
            return true;
        }
        return false;
    }

    private bool DetectByHearing()
    {
        if (Config == null) return false;

        Collider[] hits = _playerLayer == 0
            ? Physics.OverlapSphere(transform.position, Config.HearingRadius)
            : Physics.OverlapSphere(transform.position, Config.HearingRadius, _playerLayer);

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;

            Transform target = hit.transform;

            // Cache PlayerInputReader từ player
            if (_playerInputReader == null)
                _playerInputReader = target.GetComponentInParent<PlayerInputReader>();

            // Khi player đang sneak: không tạo tiếng động → không bị nghe thấy
            if (_playerInputReader != null && _playerInputReader.IsSneakHeld) continue;

            // Khi không sneak: chỉ nghe thấy nếu player đang thực sự di chuyển
            var cc = target.GetComponentInParent<CharacterController>();
            if (cc != null && cc.velocity.magnitude < Config.HearingNoiseThreshold) continue;

            _player = target;
            return true;
        }
        return false;
    }
    private void BeginChase()
    {
        if (Config == null) return;

        _state = State.Chasing;
        _agent.isStopped = false;
        _agent.speed = Config.MoveSpeed;
        _lostTargetTimer = 0f;
    }

    private void UpdateChase()
    {
        if (Config == null) return;
        if (_player == null) { BeginWander(); return; }

        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist > Config.ChaseRadius) { BeginWander(); return; }

        bool specialReady = !_isAttacking
                         && _lastClawTime > -999f
                         && Time.time >= _lastClawTime + Config.SpecialCooldown
                         && dist <= Config.SpecialRange;

        if (specialReady)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
            _agent.velocity = Vector3.zero;
            if (_anim != null) _anim.SetIdle();
            StartCoroutine(PerformSpecialAttack());
            return;
        }

        if (dist <= Config.AttackRange)
        {
            // Trong tầm đánh: đứng yên
            _agent.isStopped = true;
            _agent.ResetPath();
            _agent.velocity = Vector3.zero;
            if (_anim != null) _anim.SetIdle();

            // Đủ 5 giây kể từ lần đầu dùng claw → đánh dấu jump ready
            if (!_jumpAttackReady && !_isJumpAttacking
                && _firstClawTime > 0f
                && Time.time >= _firstClawTime + _jumpAttackDelay)
            {
                _jumpAttackReady = true;
            }

            // Kích hoạt JumpAttack: chờ claw hiện tại xong rồi mới nhảy
            if (_jumpAttackReady && !_isAttacking && !_isJumpAttacking)
            {
                _jumpAttackReady = false;
                _firstClawTime = -999f; // reset để chu kỳ tiếp theo đếm lại
                StartCoroutine(JumpAttackRoutine());
                return;
            }

            TryClawAttack();
            return;
        }

        // Player thoát tầm: reset hoàn toàn chu kỳ JumpAttack
        _firstClawTime = -999f;
        _jumpAttackReady = false;

        if (_isAttacking)
        {
            _attackCancelled = true;
            _isAttacking = false;
            if (_anim != null) _anim.CancelAttack();
        }

        _agent.isStopped = false;
        _agent.SetDestination(_player.position);
        if (_anim != null) _anim.SetRunning(true);

        bool canDetect = CanStillDetect();
        if (!canDetect)
        {
            _lostTargetTimer += Time.deltaTime;
            if (_lostTargetTimer >= Config.LoseTargetTime) BeginWander();
        }
        else _lostTargetTimer = 0f;
    }

    private bool CanStillDetect()
    {
        if (Config == null || _player == null) return false;
        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist <= Config.VisionRange)
        {
            Vector3 dir = (_player.position - transform.position).normalized;
            Ray ray = new Ray(transform.position + Vector3.up * 0.8f, dir);
            if (_obstacleLayer == 0 || !Physics.Raycast(ray, dist, _obstacleLayer)) return true;
        }
        if (dist <= Config.HearingRadius) return true;
        return false;
    }

    private void BeginWander()
    {
        if (Config == null) return;

        _state = State.Wandering;
        _agent.isStopped = false;
        _agent.speed = Config.WanderSpeed;
        _player = null;
        _lostTargetTimer = 0f;
        _firstClawTime = -999f;
        _jumpAttackReady = false;
        if (_anim != null) _anim.SetIdle();
    }

    private void TryClawAttack()
    {
        if (Config == null) return;
        if (_isAttacking) return;
        if (Time.time < _lastAttackTime + Config.AttackCooldown) return;
        if (_isDead) return;

        // Ghi nhận lần đầu tiên dùng claw — để đếm 5 giây cho JumpAttack
        if (_firstClawTime < 0f)
            _firstClawTime = Time.time;

        _isAttacking = true;
        _attackCancelled = false;
        _lastAttackTime = Time.time;

        if (_anim != null) _anim.TriggerAttackClaw();
        StartCoroutine(ClawComboRoutine());
    }

    private IEnumerator ClawComboRoutine()
    {
        yield return new WaitForSeconds(0.4f);
        if (!_attackCancelled) ApplyClawDamage();

        yield return new WaitForSeconds(0.5f);
        if (_attackCancelled) yield break;   // thoát sớm nếu đã cancel

        yield return new WaitForSeconds(0.35f);
        if (!_attackCancelled) ApplyClawDamage();

        yield return new WaitForSeconds(0.5f);

        if (!_attackCancelled)
        {
            _isAttacking = false;
            _lastClawTime = Time.time;
        }
    }

    private void ApplyClawDamage()
    {
        if (Config == null || _isDead || _player == null) return;
        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist > Config.AttackRange + 0.5f) return;

        var damageable = _player.GetComponentInParent<IDamageable>();
        if (damageable != null && !damageable.IsDead)
            damageable.TakeDamage(Config.BaseDamage, gameObject);
    }

    /// <summary>
    /// JumpAttack tại chỗ — zombie không di chuyển, chỉ giậm chân xuống.
    /// Damage + stun được xử lý bởi HandleJumpAttackImpact khi Animation Event fired.
    /// </summary>
    private IEnumerator JumpAttackRoutine()
    {
        _isJumpAttacking = true;
        _isAttacking = true;
        _attackCancelled = false;

        // Khoá agent hoàn toàn — không trượt theo player
        _agent.isStopped = true;
        _agent.ResetPath();
        _agent.velocity = Vector3.zero;

        if (_anim != null) _anim.TriggerJumpAttack();

        // Chờ animation kết thúc (thời gian ước lượng theo clip)
        // Damage thực sự xử lý qua Animation Event → HandleJumpAttackImpact
        yield return new WaitForSeconds(1.8f);

        _isJumpAttacking = false;
        _isAttacking = false;
        _lastClawTime = Time.time; // reset cooldown để không bị special ngay sau jump
    }

    /// <summary>
    /// Được gọi bởi Animation Event tại frame chân chạm đất trong clip JumpAttack.
    /// Áp damage, stun và spawn effect tại đây.
    /// </summary>
    private void HandleJumpAttackImpact()
    {
        if (_isDead) return;

        // Spawn effect tại vị trí chân zombie
        if (_jumpImpactEffectPrefab != null)
            ObjectPool.Instance.Get(_jumpImpactEffectPrefab, transform.position, Quaternion.identity);

        // Kiểm tra player vẫn trong tầm tại thời điểm impact
        if (_player == null) return;
        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist > _jumpLandRadius) return;

        var damageable = _player.GetComponentInParent<IDamageable>();
        if (damageable == null || damageable.IsDead) return;

        damageable.TakeDamage(_jumpDamage, gameObject);

        var stunnable = _player.GetComponentInParent<IStunnable>();
        stunnable?.ApplyStun(_jumpStunDuration);
    }

    private IEnumerator PerformSpecialAttack()
    {
        _isAttacking = true;
        _attackCancelled = false;

        if (_anim != null) _anim.TriggerSpecialAttack();

        yield return new WaitForSeconds(0.6f);

        if (!_attackCancelled && !_isDead && _player != null)
            FireAcid();

        yield return new WaitForSeconds(1f);

        if (!_attackCancelled)
        {
            _isAttacking = false;
            _lastClawTime = Time.time;
        }
    }

    private void FireAcid()
    {
        if (Config == null) return;
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
            projectile.Initialize(_player, Config.SpecialDamage, Config.AcidSpeed, Config.AcidLifetime, gameObject);
    }

    public void OnTakeDamage(Transform attacker)
    {
        if (_isDead) return;
        _player = attacker;
        if (_state != State.Chasing) BeginChase();
    }

    public void Die()
    {
        if (_isDead) return;
        _isDead = true;
        _isAttacking = false;
        _state = State.Dead;

        StopAllCoroutines();
        _agent.isStopped = true;
        _agent.ResetPath();
        _agent.enabled = false; // tắt hẳn NavMeshAgent — tránh bị các agent khác đẩy (avoidance) khi đã chết

        // Tắt CHỈ collider gốc (collider phát hiện/va chạm chính của zombie lúc còn sống).
        // KHÔNG đụng tới collider trên các bone ragdoll con — chúng cần giữ nguyên trạng thái
        // để ragdoll va chạm được với plane/địa hình sau khi TriggerDeath() bật chúng lên.
        var rootCol = GetComponent<Collider>();
        if (rootCol != null) rootCol.enabled = false;

        // TriggerDeath() sẽ bật Rigidbody (isKinematic = false) + Collider trên các bone ragdoll.
        // Phải gọi SAU khi đã tắt rootCol để không bị ghi đè/tắt nhầm.
        if (_anim != null) { _anim.SetIdle(); _anim.TriggerDeath(); }

        // Đổi sang layer "Corpse" — xem giải thích chi tiết trong ZombieController.cs
        SetLayerRecursive(transform, LayerMask.NameToLayer("Corpse"));

        float despawnDelay = Config != null ? Config.DespawnDelay : 120f;
        ObjectPool.Instance.ReturnDelayed(gameObject, despawnDelay);
        if (_spawnPoint != null)
            _spawnPoint.Invoke("OnZombieFatDespawned", despawnDelay);
    }

    /// <summary>
    /// Đổi layer của object gốc + toàn bộ object con (các bone ragdoll) sang layer chỉ định.
    /// Xem giải thích chi tiết trong ZombieController.cs
    /// </summary>
    private void SetLayerRecursive(Transform root, int layer)
    {
        if (layer < 0) return;
        root.gameObject.layer = layer;
        foreach (Transform child in root)
            SetLayerRecursive(child, layer);
    }

    private void OnDrawGizmosSelected()
    {
        if (Config == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, Config.HearingRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, Config.VisionRange);
        Gizmos.color = new Color(0.5f, 0f, 1f);
        Gizmos.DrawWireSphere(transform.position, Config.SpecialRange);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, Config.ChaseRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _jumpLandRadius);
        float half = Config.VisionAngle * 0.5f;
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position,
            Quaternion.Euler(0, -half, 0) * transform.forward * Config.VisionRange);
        Gizmos.DrawRay(transform.position,
            Quaternion.Euler(0, half, 0) * transform.forward * Config.VisionRange);
    }
}