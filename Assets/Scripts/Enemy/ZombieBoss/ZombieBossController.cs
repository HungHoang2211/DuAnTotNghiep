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
    [SerializeField] private float _jumpDamage = 40f;
    [SerializeField] private float _attackCooldown = 1.5f;
    [SerializeField] private float _jumpDelay = 10f;
    [SerializeField] private float _stunDuration = 3f;
    [SerializeField] private float _jumpLandRadius = 1.5f;

    [Header("Jump")]
    [SerializeField] private float _jumpNearThreshold = 3f;
    [SerializeField] private float _jumpTravelDuration = 0.5f;
    [SerializeField] private float _jumpArcHeight = 3f;

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
    private bool _hasSummoned;
    private bool _hasJumped;
    private float _combatStartTime = -1f;
    private float _lastAttackTime = -999f;
    private bool _isInCombat;
    private bool _attackCancelled;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _anim = GetComponent<ZombieBossAnimatorController>();
        _stats = GetComponent<EnemyStats>();

        _stats.OnDeath += HandleDeath;
        _stats.OnDamagedBy += HandleDamagedBy;
    }

    private void OnDestroy()
    {
        if (_stats == null) return;
        _stats.OnDeath -= HandleDeath;
        _stats.OnDamagedBy -= HandleDamagedBy;
    }

    public void Initialize(ZombieBossSpawnPoint spawnPoint)
    {
        _spawnPoint = spawnPoint;
        _isDead = false;
        _isActing = false;
        _hasSummoned = false;
        _hasJumped = false;
        _combatStartTime = -1f;
        _lastAttackTime = -999f;
        _state = State.Wandering;
        _player = null;
        _attackCancelled = false;

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
            UpdateChase();

        float speed = _agent.velocity.magnitude / _chaseSpeed;
        if (_anim != null) _anim.SetMoveSpeed(speed);
    }

    private void CheckCancelAttack()
    {
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
            TryAttack(dist);
            return;
        }

        // Player thoát tầm: cancel attack nếu đang đánh
        if (_isActing)
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

        bool jumpReady = !_hasJumped && Time.time >= _combatStartTime + _jumpDelay;
        if (jumpReady)
        {
            _hasJumped = true;
            StartCoroutine(JumpAttackRoutine(distToPlayer));
            return;
        }

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

    private IEnumerator JumpAttackRoutine(float distToPlayer)
    {
        _attackCancelled = false;
        _isActing = true;
        _lastAttackTime = Time.time;

        if (_anim != null) _anim.TriggerJumpAttack();

        yield return new WaitForSeconds(0.4f);

        if (_player == null || _isDead)
        {
            yield return new WaitForSeconds(0.8f);
            _isActing = false;
            yield break;
        }

        if (distToPlayer <= _jumpNearThreshold)
        {
            // Player gần: giậm xuống tại chỗ
        }
        else
        {
            // Player xa: bay tới vị trí player
            Vector3 landPosition = _player.position;
            yield return StartCoroutine(TravelToPosition(landPosition, _jumpTravelDuration));
        }

        // Áp damage vùng đáp
        Collider[] hits = Physics.OverlapSphere(transform.position, _jumpLandRadius, _playerLayer);
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;
            DamagePlayer(_jumpDamage, true);
            break;
        }

        yield return new WaitForSeconds(0.6f);
        _isActing = false;
    }

    private IEnumerator TravelToPosition(Vector3 target, float duration)
    {
        Vector3 start = transform.position;
        float elapsed = 0f;

        _agent.isStopped = true;
        _agent.updatePosition = false;
        _agent.updateRotation = false;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            Vector3 flat = Vector3.Lerp(start, target, t);
            float arc = Mathf.Sin(t * Mathf.PI) * _jumpArcHeight;
            transform.position = new Vector3(flat.x, start.y + arc, flat.z);

            Vector3 dir = target - transform.position; dir.y = 0;
            if (dir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(dir);

            yield return null;
        }

        if (NavMesh.SamplePosition(target, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            transform.position = hit.position;

        _agent.Warp(transform.position);
        _agent.updatePosition = true;
        _agent.updateRotation = true;
        _agent.isStopped = false;
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
        _hasJumped = false;
        _isInCombat = false;
        _attackCancelled = true;
    }

    private void CheckSummon()
    {
        if (_hasSummoned || _stats == null) return;
        if (_stats.HP <= _stats.MaxHP * 0.5f)
            StartCoroutine(SummonRoutine());
    }

    private IEnumerator SummonRoutine()
    {
        _hasSummoned = true;
        _isActing = true;

        if (_anim != null) _anim.TriggerHowl();
        yield return new WaitForSeconds(0.5f);

        SpawnMinion(_summonPoint1);
        SpawnMinion(_summonPoint2);

        yield return new WaitForSeconds(1.5f);
        _isActing = false;
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
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _jumpNearThreshold);
    }
}