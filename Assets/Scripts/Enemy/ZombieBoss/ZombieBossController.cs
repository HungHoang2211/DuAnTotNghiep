using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using SimpleSurvival.Combat;
using SimpleSurvival.Stats;
using SimpleSurvival.Input;
using SimpleSurvival.Core;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyStats))]
public class ZombieBossController : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private LayerMask _playerLayer;
    [SerializeField] private LayerMask _obstacleLayer;
    [SerializeField] private float _detectionRange = 12f;
    [SerializeField] private float _attackRange = 2f;

    [Header("Combat")]
    [SerializeField] private float _baseDamage = 25f;
    [SerializeField] private float _attackCooldown = 1.5f;
    [SerializeField] private float _stunDuration = 3f;

    [Header("Movement")]
    [SerializeField] private float _wanderRadius = 6f;
    [SerializeField] private float _wanderIntervalMin = 2f;
    [SerializeField] private float _wanderIntervalMax = 5f;
    [SerializeField] private float _chaseSpeed = 4f;
    [SerializeField] private float _wanderSpeed = 1.5f;

    [Header("Summon")]
    [SerializeField] private GameObject _minionPrefab;
    [SerializeField] private Transform _summonPoint1;
    [SerializeField] private Transform _summonPoint2;
    [SerializeField] private GameObject _summonEffectPrefab;

    [Header("Laser Attack")]
    [Tooltip("Prefab laser AOE (phải có ZombieBossSkill + PooledObject script)")]
    [SerializeField] private GameObject _orbPrefab;
    [Tooltip("Khi đang đuổi mà không tấn công được, cứ sau bao lâu thì bắn laser (giây)")]
    [SerializeField] private float _chaseOrbInterval = 10f;

    [Header("Spawn")]
    [SerializeField] private float _despawnDelay = 180f;

    private enum State { Wandering, Chasing, Dead }
    private State _state = State.Wandering;

    private NavMeshAgent _agent;
    private ZombieBossAnimatorController _anim;
    private EnemyStats _stats;
    private ZombieBossSpawnPoint _spawnPoint;
    private Transform _player;

    private bool _isDead;
    private bool _isActing;
    private bool _isSummoning;   // true khi đang trong SummonRoutine — không được cancel
    private bool _hasSummoned;
    private float _combatStartTime = -1f;
    private float _lastAttackTime = -999f;
    private bool _isInCombat;
    private bool _attackCancelled;
    private bool _howlFinished;

    // Orb chase: đếm thời gian đuổi mà không tấn công được
    private float _chaseWithoutAttackTimer = 0f;
    private bool _isChaseOrbCooling = false;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _anim = GetComponent<ZombieBossAnimatorController>();
        _stats = GetComponent<EnemyStats>();

        _stats.OnDeath += HandleDeath;
        _stats.OnDamagedBy += HandleDamagedBy;

        if (_anim != null)
        {
            _anim.OnHowlSpawn += HandleHowlSpawn;
            _anim.OnHowlFinished += () => _howlFinished = true;
        }
    }

    private void OnDestroy()
    {
        if (_stats == null) return;
        _stats.OnDeath -= HandleDeath;
        _stats.OnDamagedBy -= HandleDamagedBy;

        if (_anim != null)
            _anim.OnHowlSpawn -= HandleHowlSpawn;
    }

    public void Initialize(ZombieBossSpawnPoint spawnPoint)
    {
        _spawnPoint = spawnPoint;
        _isDead = false;
        _isActing = false;
        _hasSummoned = false;
        _combatStartTime = -1f;
        _lastAttackTime = -999f;
        _state = State.Wandering;
        _player = null;
        _attackCancelled = false;
        _howlFinished = false;
        _isSummoning = false;
        _chaseWithoutAttackTimer = 0f;
        _isChaseOrbCooling = false;

        _agent.isStopped = false;
        _agent.speed = _wanderSpeed;
        _agent.stoppingDistance = 0.2f;
        _agent.angularSpeed = 360f;
        _agent.acceleration = 12f;
        _agent.updateRotation = true;

        var col = GetComponent<Collider>();
        if (col != null) col.enabled = true;

        if (_anim != null) _anim.ResetForSpawn();

        StopAllCoroutines();
        StartCoroutine(WanderRoutine());
        StartCoroutine(DetectionRoutine());
    }

    private void Update()
    {
        if (_isDead) return;

        if (_isActing)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
            _agent.velocity = Vector3.zero;
            FacePlayer();
            if (_anim != null) _anim.SetMoveSpeed(_isInCombat ? 0.01f : 0f);

            // Vẫn check player thoát tầm dù đang acting
            CheckCancelAttack();
            return;
        }

        if (_state == State.Chasing)
        {
            UpdateChase();
            UpdateChaseOrbTimer();
        }

        float speed = _agent.velocity.magnitude / _chaseSpeed;
        if (_anim != null) _anim.SetMoveSpeed(speed);
    }

    private void CheckCancelAttack()
    {
        // Đang summon: KHÔNG được cancel — phải đợi howl xong
        if (_isSummoning) return;

        if (_player == null)
        {
            _attackCancelled = true;
            _isActing = false;
            _isInCombat = false;
            return;
        }

        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist > _attackRange - 0.3f)
        {
            _attackCancelled = true;
            _isActing = false;
            _isInCombat = false;
        }
    }
    private void FacePlayer()
    {
        if (_player == null) return;
        Vector3 dir = _player.position - transform.position;
        dir.y = 0;
        if (dir == Vector3.zero) return;
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, Quaternion.LookRotation(dir), 360f * Time.deltaTime);
    }

    private void UpdateChase()
    {
        if (_player == null) { BeginWander(); return; }

        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist > _detectionRange) { BeginWander(); return; }

        if (dist <= _attackRange)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
            // Player vào tầm đánh — reset timer đuổi
            _chaseWithoutAttackTimer = 0f;
            TryAttack(dist);
            return;
        }

        // Player thoát tầm: cancel attack nếu đang đánh (nhưng không cancel khi đang summon)
        if (_isActing && !_isSummoning)
        {
            _attackCancelled = true;
            _isActing = false;
            _isInCombat = false;
        }

        _agent.isStopped = false;
        _agent.SetDestination(_player.position);
    }

    private void TryAttack(float distToPlayer)
    {
        if (_isActing || _isDead) return;

        if (_combatStartTime < 0f)
            _combatStartTime = Time.time;

        if (Time.time >= _lastAttackTime + _attackCooldown)
            StartCoroutine(NormalAttackRoutine());
    }

    private IEnumerator NormalAttackRoutine()
    {
        _isActing = true;
        _isInCombat = true;
        _attackCancelled = false;
        _lastAttackTime = Time.time;

        // 1 trigger kích hoạt cả combo claw_left → claw_right
        if (_anim != null) _anim.TriggerAttackClaw();

        // Damage tay trái
        yield return new WaitForSeconds(0.4f);
        ApplyNormalDamage();

        yield return new WaitForSeconds(0.5f);
        if (_isDead) { _isActing = false; yield break; }

        // Damage tay phải
        yield return new WaitForSeconds(0.4f);
        ApplyNormalDamage();

        yield return new WaitForSeconds(0.5f);

        _isActing = false;
        _isInCombat = false;

        // Nếu lúc đánh HP đã tụt xuống ngưỡng triệu hồi nhưng bị CheckSummon() chặn vì
        // _isActing — thử lại ngay khi vừa hết đòn, không cần đợi player đánh tiếp.
        CheckSummon();
    }

    private void ApplyNormalDamage()
    {
        if (_isDead || _player == null) return;
        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist > _attackRange + 0.5f) return;

        var damageable = _player.GetComponentInParent<IDamageable>();
        if (damageable != null && !damageable.IsDead)
            damageable.TakeDamage(_baseDamage, gameObject);
    }

    private void DamagePlayer(float amount, bool stun)
    {
        if (_player == null) return;

        var damageable = _player.GetComponentInParent<IDamageable>();
        if (damageable == null || damageable.IsDead) return;

        damageable.TakeDamage(amount, gameObject);

        if (stun)
        {
            var stunnable = _player.GetComponentInParent<IStunnable>();
            stunnable?.ApplyStun(_stunDuration);
        }
    }

    private IEnumerator WanderRoutine()
    {
        while (!_isDead)
        {
            if (_state == State.Wandering && !_isActing)
            {
                Vector3 target = GetRandomNavMeshPoint(transform.position, _wanderRadius);
                _agent.SetDestination(target);

                float elapsed = 0f;
                while (_state == State.Wandering && elapsed < 8f)
                {
                    elapsed += Time.deltaTime;
                    if (!_agent.pathPending && _agent.remainingDistance < 0.3f) break;
                    yield return null;
                }
            }
            yield return new WaitForSeconds(Random.Range(_wanderIntervalMin, _wanderIntervalMax));
        }
    }

    private IEnumerator DetectionRoutine()
    {
        while (!_isDead)
        {
            yield return new WaitForSeconds(0.2f);
            if (_state != State.Wandering) continue;

            if (DetectPlayer())
                BeginChase();
        }
    }

    private bool DetectPlayer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _detectionRange, _playerLayer);
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;
            _player = hit.transform;
            return true;
        }
        return false;
    }

    private void BeginChase()
    {
        _state = State.Chasing;
        _agent.isStopped = false;
        _agent.speed = _chaseSpeed;
    }

    private void BeginWander()
    {
        _state = State.Wandering;
        _agent.isStopped = false;
        _agent.speed = _wanderSpeed;
        _player = null;
        _combatStartTime = -1f;
        _isInCombat = false;
        _attackCancelled = true;
        _chaseWithoutAttackTimer = 0f;
    }

    /// <summary>
    /// Đếm thời gian boss đang đuổi mà player vẫn ngoài _attackRange.
    /// Cứ sau _chaseOrbInterval giây thì bắn 1 quả orb về phía player.
    /// Timer reset lại sau mỗi lần bắn hoặc khi player vào tầm đánh.
    /// </summary>
    private void UpdateChaseOrbTimer()
    {
        if (_isDead || _isActing || _player == null) return;

        float dist = Vector3.Distance(transform.position, _player.position);

        // Chỉ đếm khi player đang NGOÀI tầm đánh (đang bị đuổi)
        if (dist > _attackRange)
        {
            _chaseWithoutAttackTimer += Time.deltaTime;
            if (_chaseWithoutAttackTimer >= _chaseOrbInterval)
            {
                _chaseWithoutAttackTimer = 0f;
                StartCoroutine(ChaseOrbRoutine());
            }
        }
    }

    /// <summary>
    /// Dừng lại, bắn laser xuống vị trí player hiện tại rồi tiếp tục đuổi.
    /// Player vẫn có thể né nếu di chuyển trong thời gian warning của laser.
    /// </summary>
    private IEnumerator ChaseOrbRoutine()
    {
        if (_isDead || _isActing) yield break;

        _isActing = true;
        _agent.isStopped = true;
        _agent.ResetPath();

        // Quay mặt về phía player ngắn
        float faceTime = 0.3f;
        float elapsed = 0f;
        while (elapsed < faceTime)
        {
            FacePlayer();
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Bắn laser xuống vị trí player lúc này
        ShootLaser();

        yield return new WaitForSeconds(0.2f);

        _isActing = false;
        if (!_isDead && _state == State.Chasing)
            _agent.isStopped = false;

        CheckSummon();
    }

    private void ShootLaser()
    {
        if (_orbPrefab == null || _player == null) return;

        // Laser rơi từ trên xuống tại vị trí player hiện tại
        var obj = ObjectPool.Instance.Get(_orbPrefab, _player.position, Quaternion.identity);
        var laser = obj.GetComponent<ZombieBossSkill>();
        if (laser != null)
            laser.Launch(_player.position, gameObject);
    }

    private void CheckSummon()
    {
        if (_hasSummoned || _stats == null) return;
        if (_isActing) return; // đang bận (đánh thường/laser...) — không cướp ngang, sẽ được check lại khi action hiện tại kết thúc
        if (_stats.HP <= _stats.MaxHP * 0.5f)
            StartCoroutine(SummonRoutine());
    }

    private IEnumerator SummonRoutine()
    {
        _hasSummoned = true;
        _isActing = true;
        _isSummoning = true;
        _howlFinished = false;

        if (_anim != null) _anim.TriggerHowl();

        // Chỉ chờ animation howl chạy hết (Animation Event: HowlFinished)
        // Việc spawn minion được xử lý trực tiếp bởi HandleHowlSpawn khi Animation Event HowlSpawn fired
        float timeout = 5f;
        float elapsed = 0f;
        while (!_howlFinished && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(0.5f);
        _isSummoning = false;
        _isActing = false;
    }

    /// <summary>
    /// Được gọi trực tiếp từ event OnHowlSpawn khi Animation Event HowlSpawn fired.
    /// Spawn ngay lập tức, không qua coroutine để tránh delay 1 frame.
    /// </summary>
    private void HandleHowlSpawn()
    {
        if (_isDead) return;
        SpawnMinion(_summonPoint1);
        SpawnMinion(_summonPoint2);

        // KHÔNG bắn laser ở đây nữa: laser chỉ được dùng khi player đang chạy thoát
        // (ngoài _attackRange, không bị boss tác động) — xem ChaseOrbRoutine/UpdateChaseOrbTimer.
        // Trước đây code bắn laser dựa vào _orbReady (tích lũy thời gian player ở GẦN trong
        // lúc cận chiến) ngay tại frame summon minion, nên laser và effect triệu hồi luôn
        // xuất hiện cùng lúc — sai với thiết kế mong muốn.
    }

    private void SpawnMinion(Transform point)
    {
        if (_minionPrefab == null || point == null) return;

        if (_summonEffectPrefab != null)
        {
            var effect = ObjectPool.Instance.Get(_summonEffectPrefab, point.position, Quaternion.identity);
            ObjectPool.Instance.ReturnDelayed(effect, 2f);
        }

        var obj = ObjectPool.Instance.Get(_minionPrefab, point.position, point.rotation);
        var controller = obj.GetComponent<ZombieController>();
        if (controller != null)
            controller.Initialize(null);
    }

    private void HandleDeath()
    {
        if (_isDead) return;
        _isDead = true;
        _isActing = false;
        _state = State.Dead;

        StopAllCoroutines();
        _agent.isStopped = true;
        _agent.ResetPath();
        _agent.enabled = false; // tắt hẳn NavMeshAgent — tránh bị các agent khác đẩy (avoidance) khi đã chết, và tránh xung đột với ragdoll physics

        // Tắt TẤT CẢ collider (gốc + con) NGAY khi chết — đóng lỗ hổng va chạm trong khoảng
        // thời gian chờ trước khi ragdoll kích hoạt (ragdoll sẽ tự enable lại collider bone nó cần)
        foreach (var c in GetComponentsInChildren<Collider>())
            c.enabled = false;

        Vector3 forceDir = _player != null
            ? (transform.position - _player.position).normalized + Vector3.up
            : Vector3.up;

        if (_anim != null) _anim.TriggerDeath();
        StartCoroutine(ActivateRagdollDelayed(forceDir));

        ObjectPool.Instance.ReturnDelayed(gameObject, _despawnDelay);
        if (_spawnPoint != null)
            _spawnPoint.Invoke("OnBossDespawned", _despawnDelay);
    }

    private IEnumerator ActivateRagdollDelayed(Vector3 forceDir)
    {
        yield return new WaitForSeconds(0.15f);
        if (_anim != null) _anim.ActivateRagdoll(forceDir);
        SetLayerRecursive(transform, LayerMask.NameToLayer("Corpse"));
    }

    private void HandleDamagedBy(GameObject source)
    {
        if (source == null || _isDead) return;
        _player = source.transform;

        CheckSummon();

        if (_state == State.Wandering)
            BeginChase();
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

    /// <summary>
    /// Đổi layer của object gốc + toàn bộ object con (các bone ragdoll) sang layer chỉ định.
    /// Dùng để chuyển xác chết qua layer "Corpse" — layer này được set KHÔNG va chạm với
    /// layer "Enemy"/"Player" trong Physics Layer Collision Matrix, nên enemy còn sống đi
    /// ngang qua sẽ không còn đẩy được xác ragdoll nữa, nhưng xác vẫn rơi/va đất bình thường.
    /// </summary>
    private void SetLayerRecursive(Transform root, int layer)
    {
        if (layer < 0) return; // layer "Corpse" chưa được tạo trong Project Settings
        root.gameObject.layer = layer;
        foreach (Transform child in root)
            SetLayerRecursive(child, layer);
    }

    private void OnDrawGizmosSelected() 
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _attackRange);
    }
}