using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using SimpleSurvival.Combat;
using SimpleSurvival.Stats;
using Xyla.Core;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyStats))]
public class ZombieController : MonoBehaviour
{
    [Header("Variant")]
    [Tooltip("Bật để Zombie dùng run animation khi chase. Tắt = walk. Tương ứng với 2 prefab variant.")]
    [SerializeField] private bool _isRunner = false;

    [Header("Detection Layers")]
    [SerializeField] private LayerMask _playerLayer;
    [SerializeField] private LayerMask _obstacleLayer;

    private enum State { Wandering, Alerting, Chasing, Dead }
    private State _state = State.Wandering;

    private NavMeshAgent _agent;
    private ZombieAnimatorController _anim;
    private ZombieSpawnPoint _spawnPoint;
    private EnemyStats _stats;
    private Transform _player;

    private bool _isDead = false;
    private bool _isAttacking = false;
    private float _lastAttackTime = -999f;
    private float _lostTargetTimer = 0f;
    private Vector3 _homePosition;

    private EnemyStatsConfig Config => _stats != null ? _stats.EnemyConfig : null;

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
    }

    private void OnDestroy()
    {
        if (_stats != null)
        {
            _stats.OnDeath -= HandleDeath;
            _stats.OnDamagedBy -= HandleDamagedBy;
        }
    }

    public void Initialize(ZombieSpawnPoint spawnPoint)
    {
        if (Config == null)
        {
            Debug.LogError($"[{name}] EnemyStatsConfig missing on EnemyStats", this);
            return;
        }

        _spawnPoint = spawnPoint;
        _isDead = false;
        _isAttacking = false;
        _state = State.Wandering;
        _lostTargetTimer = 0f;
        _player = null;
        _homePosition = transform.position;
        _lastAttackTime = -999f;

        _agent.isStopped = false;
        _agent.speed = Config.WanderSpeed;
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

    private void HandleDeath() => Die();

    private void HandleDamagedBy(GameObject source)
    {
        if (source != null)
            OnTakeDamage(source.transform);
    }

    private void Update()
    {
        if (_isDead) return;
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

    private void PlayChaseAnimation()
    {
        if (_anim == null) return;
        if (_isRunner)
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
            if (detected) StartCoroutine(AlertRoutine());
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
            _player = hit.transform;
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

        if (Random.value <= Config.HowlChance)
        {
            if (_anim != null) _anim.SetHowling(true);
            yield return new WaitForSeconds(Config.HowlDuration);
            if (_anim != null) _anim.SetHowling(false);
        }

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
        if (Config == null) return;
        if (_player == null) { BeginWander(); return; }

        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist > Config.ChaseRadius) { BeginWander(); return; }

        if (dist <= Config.AttackRange)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
            _agent.velocity = Vector3.zero;
            if (_anim != null) _anim.SetIdle();
            TryAttack();
            return;
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
        if (_anim != null) { _anim.SetHowling(false); _anim.SetIdle(); }
    }

    private void TryAttack()
    {
        if (Config == null) return;
        if (_isAttacking) return;
        if (Time.time < _lastAttackTime + Config.AttackCooldown) return;
        if (_isDead) return;

        _isAttacking = true;
        _lastAttackTime = Time.time;

        if (_anim != null) _anim.TriggerAttack();
        StartCoroutine(ApplyDamage());
    }

    private IEnumerator ApplyDamage()
    {
        yield return new WaitForSeconds(0.4f);

        if (Config != null && !_isDead && _player != null)
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
        _isAttacking = false;
    }

    public void OnTakeDamage(Transform attacker)
    {
        if (_isDead) return;
        _player = attacker;
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

        if (_anim != null) { _anim.SetHowling(false); _anim.SetIdle(); _anim.TriggerDeath(); }

        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        float despawnDelay = Config != null ? Config.DespawnDelay : 120f;
        ObjectPool.Instance.ReturnDelayed(gameObject, despawnDelay);
        if (_spawnPoint != null) _spawnPoint.Invoke("OnZombieDespawned", despawnDelay);
    }

    private void OnDrawGizmosSelected()
    {
        if (Config == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, Config.HearingRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, Config.VisionRange);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, Config.ChaseRadius);
        Gizmos.color = Color.cyan;
        float half = Config.VisionAngle * 0.5f;
        Gizmos.DrawRay(transform.position,
            Quaternion.Euler(0, -half, 0) * transform.forward * Config.VisionRange);
        Gizmos.DrawRay(transform.position,
            Quaternion.Euler(0, half, 0) * transform.forward * Config.VisionRange);
    }
}