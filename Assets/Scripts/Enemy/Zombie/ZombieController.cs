using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using SimpleSurvival.Combat;
using SimpleSurvival.Input;
using SimpleSurvival.Stats;
using SimpleSurvival.Core;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyStats))]
public class ZombieController : MonoBehaviour, ISpawnableEnemy
{
    public enum Variant { Normal, Fat }

    [Header("Variant")]
    [Tooltip("Normal = Zombie thường. Fat = ZombieFat (claw combo + special attack + jump attack).")]
    [SerializeField] private Variant _variant = Variant.Normal;

    [Header("Detection Layers")]
    [SerializeField] private LayerMask _playerLayer;
    [SerializeField] private LayerMask _obstacleLayer;

    [Header("[Fat] Special Effect References")]
    [Tooltip("[Chỉ Variant.Fat] Prefab effect axit bắn từ miệng. Cần có Rigidbody + Collider(IsTrigger).")]
    [SerializeField] private GameObject _acidEffectPrefab;
    [Tooltip("[Chỉ Variant.Fat] Vị trí miệng zombie — kéo bone đầu hoặc empty object tại miệng.")]
    [SerializeField] private Transform _mouthTransform;

    [Header("[Fat] Jump Attack")]
    [Tooltip("[Chỉ Variant.Fat] Prefab effect xuất hiện khi chân chạm đất (gắn qua Animation Event).")]
    [SerializeField] private GameObject _jumpImpactEffectPrefab;
    [Tooltip("[Chỉ Variant.Fat] Sau bao nhiêu giây liên tục đứng trong tầm _isAttacking thì dùng JumpAttack.")]
    [SerializeField] private float _jumpAttackDelay = 5f;
    [Tooltip("[Chỉ Variant.Fat] Bán kính vùng damage khi giậm chân.")]
    [SerializeField] private float _jumpLandRadius = 1.5f;
    [Tooltip("[Chỉ Variant.Fat] Damage của JumpAttack.")]
    [SerializeField] private float _jumpDamage = 35f;
    [Tooltip("[Chỉ Variant.Fat] Thời gian choáng áp lên player khi JumpAttack trúng.")]
    [SerializeField] private float _jumpStunDuration = 2f;

    private enum State { Wandering, Alerting, Chasing, Dead }
    private State _state = State.Wandering;

    private NavMeshAgent _agent;
    private ZombieAnimatorController _anim;
    private IEnemySpawnPoint _spawnPoint;
    private EnemyStats _stats;
    private Transform _player;

    private bool _isDead = false;
    private bool _isAttacking = false;
    private bool _attackCancelled = false;
    private float _lastAttackTime = -999f;
    private float _lostTargetTimer = 0f;
    private Vector3 _homePosition;
    private PlayerInputReader _playerInputReader;

    private float _lastClawTime = -999f;
    private float _firstClawTime = -999f;
    private bool _jumpAttackReady = false;
    private bool _isJumpAttacking = false;

    private EnemyStatsConfig Config => _stats != null ? _stats.EnemyConfig : null;
    private ZombieFatStatsConfig FatConfig => Config as ZombieFatStatsConfig;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _anim = GetComponent<ZombieAnimatorController>();
        _stats = GetComponent<EnemyStats>();

        if (_stats == null)
        {
            Debug.LogError($"[{name}] Missing EnemyStats component", this);
            return;
        }

        _stats.OnDeath += HandleDeath;
        _stats.OnDamagedBy += HandleDamagedBy;

        if (_variant == Variant.Fat && _anim != null)
            _anim.OnJumpAttackImpact += HandleJumpAttackImpact;
    }

    private void OnDestroy()
    {
        if (_stats != null)
        {
            _stats.OnDeath -= HandleDeath;
            _stats.OnDamagedBy -= HandleDamagedBy;
        }

        if (_variant == Variant.Fat && _anim != null)
            _anim.OnJumpAttackImpact -= HandleJumpAttackImpact;
    }

    public void Initialize(IEnemySpawnPoint spawnPoint)
    {
        _spawnPoint = spawnPoint;
        InitializeCommon();
    }

    private void InitializeCommon()
    {
        if (Config == null)
        {
            Debug.LogError($"[{name}] EnemyStatsConfig missing on EnemyStats", this);
            return;
        }

        _isDead = false;
        _isAttacking = false;
        _attackCancelled = false;
        _state = State.Wandering;
        _lostTargetTimer = 0f;
        _player = null;
        _playerInputReader = null;
        _homePosition = transform.position;
        _lastAttackTime = -999f;

        // Reset riêng Fat
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

        if (_variant == Variant.Fat)
        {
            if (_isAttacking)
            {
                _agent.isStopped = true;
                _agent.velocity = Vector3.zero;
                SmoothRotation();
                return;
            }

            SmoothRotation();
            if (_state == State.Chasing) UpdateChase();
            return;
        }

        SmoothRotation();

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

    private void PlayChaseAnimation()
    {
        if (_anim == null || Config == null) return;
        if (Config.IsRunner)
            _anim.SetRunning(true);
        else
            _anim.SetWalking(true);
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
            if (!detected) continue;

            if (_variant == Variant.Fat)
                BeginChase();
            else
                StartCoroutine(AlertRoutine());
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

            if (_playerInputReader == null)
                _playerInputReader = target.GetComponentInParent<PlayerInputReader>();

            if (_playerInputReader != null && _playerInputReader.IsSneakHeld) continue;

            var cc = target.GetComponentInParent<CharacterController>();
            if (cc != null && cc.velocity.magnitude < Config.HearingNoiseThreshold) continue;

            _player = target;
            return true;
        }
        return false;
    }

    private IEnumerator AlertRoutine()
    {
        if (_state != State.Wandering) yield break;
        if (Config == null) yield break;

        _state = State.Alerting;

        _agent.isStopped = true;
        _agent.ResetPath();
        if (_anim != null) _anim.SetIdle();

        if (_player != null)
        {
            Vector3 dir = (_player.position - transform.position); dir.y = 0;
            if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir);
        }

        if (_anim != null) _anim.SetHowling(true);
        yield return new WaitForSeconds(Config.HowlDuration);
        if (_anim != null) _anim.SetHowling(false);

        if (!_isDead) BeginChase();
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
        if (_variant == Variant.Fat)
        {
            UpdateChase_Fat();
            return;
        }

        if (Config == null) return;
        if (_player == null) { BeginWander(); return; }

        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist > Config.ChaseRadius) { BeginWander(); return; }

        if (dist <= Config.AttackRange)
        {
            // Trong tầm tấn công: đứng yên, tấn công
            _agent.isStopped = true;
            _agent.ResetPath();
            _agent.velocity = Vector3.zero;
            if (_anim != null) _anim.SetIdle();
            TryAttack();
            return;
        }

        if (_isAttacking)
        {
            _attackCancelled = true;
            _isAttacking = false;
            if (_anim != null) _anim.CancelAttack();
        }

        _agent.isStopped = false;
        _agent.SetDestination(_player.position);
        PlayChaseAnimation();

        bool canDetect = CanStillDetect();
        if (!canDetect)
        {
            _lostTargetTimer += Time.deltaTime;
            if (_lostTargetTimer >= Config.LoseTargetTime) BeginWander();
        }
        else _lostTargetTimer = 0f;
    }

    private void UpdateChase_Fat()
    {
        var cfg = FatConfig;
        if (cfg == null) return;
        if (_player == null) { BeginWander(); return; }

        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist > cfg.ChaseRadius) { BeginWander(); return; }

        bool specialReady = !_isAttacking
                         && _lastClawTime > -999f
                         && Time.time >= _lastClawTime + cfg.SpecialCooldown
                         && dist <= cfg.SpecialRange;

        if (specialReady)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
            _agent.velocity = Vector3.zero;
            if (_anim != null) _anim.SetIdle();
            StartCoroutine(PerformSpecialAttack());
            return;
        }

        if (dist <= cfg.AttackRange)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
            _agent.velocity = Vector3.zero;
            if (_anim != null) _anim.SetIdle();

            if (!_jumpAttackReady && !_isJumpAttacking
                && _firstClawTime > 0f
                && Time.time >= _firstClawTime + _jumpAttackDelay)
            {
                _jumpAttackReady = true;
            }

            if (_jumpAttackReady && !_isAttacking && !_isJumpAttacking)
            {
                _jumpAttackReady = false;
                _firstClawTime = -999f;
                StartCoroutine(JumpAttackRoutine());
                return;
            }

            TryClawAttack();
            return;
        }

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
            if (_lostTargetTimer >= cfg.LoseTargetTime) BeginWander();
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

        if (_variant == Variant.Fat)
        {
            _firstClawTime = -999f;
            _jumpAttackReady = false;
            if (_anim != null) _anim.SetIdle();
        }
        else
        {
            if (_anim != null) { _anim.SetHowling(false); _anim.SetIdle(); }
        }
    }


    private void TryAttack()
    {
        if (_variant == Variant.Fat) return;
        if (Config == null) return;
        if (_isAttacking) return;
        if (Time.time < _lastAttackTime + Config.AttackCooldown) return;
        if (_isDead) return;

        _isAttacking = true;
        _attackCancelled = false;
        _lastAttackTime = Time.time;

        if (_anim != null) _anim.TriggerAttack();
        StartCoroutine(ApplyDamage());
    }

    private IEnumerator ApplyDamage()
    {
        yield return new WaitForSeconds(0.4f);

        if (!_attackCancelled && Config != null && !_isDead && _player != null)
        {
            float dist = Vector3.Distance(transform.position, _player.position);
            if (dist <= Config.AttackRange + 0.5f)
            {
                var damageable = _player.GetComponentInParent<IDamageable>();
                if (damageable != null && !damageable.IsDead)
                    damageable.TakeDamage(Config.BaseDamage, gameObject);
            }
        }

        yield return new WaitForSeconds(0.8f);

        if (!_attackCancelled)
            _isAttacking = false;
    }


    private void TryClawAttack()
    {
        var cfg = FatConfig;
        if (cfg == null) return;
        if (_isAttacking) return;
        if (Time.time < _lastAttackTime + cfg.AttackCooldown) return;
        if (_isDead) return;
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
        if (_attackCancelled) yield break;

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
        var cfg = FatConfig;
        if (cfg == null || _isDead || _player == null) return;
        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist > cfg.AttackRange + 0.5f) return;

        var damageable = _player.GetComponentInParent<IDamageable>();
        if (damageable != null && !damageable.IsDead)
            damageable.TakeDamage(cfg.BaseDamage, gameObject);
    }

    private IEnumerator JumpAttackRoutine()
    {
        _isJumpAttacking = true;
        _isAttacking = true;
        _attackCancelled = false;

        _agent.isStopped = true;
        _agent.ResetPath();
        _agent.velocity = Vector3.zero;

        if (_anim != null) _anim.TriggerJumpAttack();

        yield return new WaitForSeconds(1.8f);

        _isJumpAttacking = false;
        _isAttacking = false;
        _lastClawTime = Time.time;
    }

    private void HandleJumpAttackImpact()
    {
        if (_isDead) return;

        if (_jumpImpactEffectPrefab != null)
            ObjectPool.Instance.Get(_jumpImpactEffectPrefab, transform.position, Quaternion.identity);

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
        var cfg = FatConfig;
        if (cfg == null) return;
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
            projectile.Initialize(_player, cfg.SpecialDamage, cfg.AcidSpeed, cfg.AcidLifetime, gameObject);
    }


    public void OnTakeDamage(Transform attacker)
    {
        if (_isDead) return;
        _player = attacker;

        if (_variant == Variant.Fat)
        {
            if (_state != State.Chasing) BeginChase();
            return;
        }

        if (_state == State.Wandering) StartCoroutine(AlertRoutine());
        else if (_state == State.Chasing) BeginChase();
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
        _agent.enabled = false;
        if (_variant == Variant.Fat)
        {
            var rootCol = GetComponent<Collider>();
            if (rootCol != null) rootCol.enabled = false;

            if (_anim != null) { _anim.SetIdle(); _anim.TriggerDeath(); }

            SetLayerRecursive(transform, LayerMask.NameToLayer("Corpse"));

            float despawnDelayFat = Config != null ? Config.DespawnDelay : 120f;
            Destroy(gameObject, despawnDelayFat);
            if (_spawnPoint != null)
                _spawnPoint.NotifyDespawned(despawnDelayFat);
            return;
        }

        SetLayerRecursive(transform, LayerMask.NameToLayer("Corpse"));

        foreach (var c in GetComponentsInChildren<Collider>())
            c.enabled = false;

        if (_anim != null) { _anim.SetHowling(false); _anim.SetIdle(); _anim.TriggerDeath(); }

        float despawnDelay = Config != null ? Config.DespawnDelay : 120f;
        Destroy(gameObject, despawnDelay);
        if (_spawnPoint != null) _spawnPoint.NotifyDespawned(despawnDelay);
    }

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

        if (_variant == Variant.Fat && FatConfig != null)
        {
            Gizmos.color = new Color(0.5f, 0f, 1f);
            Gizmos.DrawWireSphere(transform.position, FatConfig.SpecialRange);
        }

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, Config.ChaseRadius);

        if (_variant == Variant.Fat)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _jumpLandRadius);
        }

        Gizmos.color = Color.cyan;
        float half = Config.VisionAngle * 0.5f;
        Gizmos.DrawRay(transform.position,
            Quaternion.Euler(0, -half, 0) * transform.forward * Config.VisionRange);
        Gizmos.DrawRay(transform.position,
            Quaternion.Euler(0, half, 0) * transform.forward * Config.VisionRange);
    }
}