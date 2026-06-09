using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using SimpleSurvival.Combat;
using SimpleSurvival.Stats;
using Xyla.Core;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyStats))]
public class WolfController : MonoBehaviour
{
    [Header("Detection Layers")]
    [SerializeField] private LayerMask _playerLayer;
    [SerializeField] private LayerMask _obstacleLayer;

    [Header("Movement Feel")]
    [SerializeField] private float _rotationSpeed = 80f;
    [SerializeField] private float _acceleration = 10f;
    [SerializeField] private float _angularSpeed = 0f;

    private enum State { Wandering, Howling, Chasing, Attacking, Returning, Dead }
    private State _state = State.Wandering;

    private NavMeshAgent _agent;
    private WolfAnimatorController _anim;
    private WolfSpawnPoint _spawnPoint;
    private EnemyStats _stats;
    private Transform _player;

    private bool _isDead = false;
    private float _lastAttackTime = 0f;
    private float _lostTargetTimer = 0f;
    private Vector3 _homePosition;

    private WolfStatsConfig Config => _stats != null ? _stats.EnemyConfig as WolfStatsConfig : null;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _anim = GetComponent<WolfAnimatorController>();
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

    public void Initialize(WolfSpawnPoint spawnPoint)
    {
        if (Config == null)
        {
            Debug.LogError($"[{name}] WolfStatsConfig missing on EnemyStats", this);
            return;
        }

        _spawnPoint = spawnPoint;
        _isDead = false;
        _state = State.Wandering;
        _lostTargetTimer = 0f;
        _player = null;
        _homePosition = transform.position;

        _agent.isStopped = false;
        _agent.speed = Config.WalkSpeed;
        _agent.acceleration = _acceleration;
        _agent.angularSpeed = _angularSpeed;
        _agent.autoBraking = false;
        _agent.stoppingDistance = 0f;
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

        if (_anim != null) _anim.SetSpeed(_agent.velocity.magnitude);

        if (_state == State.Chasing) UpdateChase();
        if (_state == State.Returning) UpdateReturn();
    }

    private void SmoothRotation()
    {
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

    private IEnumerator WanderRoutine()
    {
        while (!_isDead)
        {
            if (Config == null) yield break;

            if (_state == State.Wandering)
            {
                _homePosition = transform.position;
                Vector3 target = GetRandomNavMeshPoint(transform.position, Config.WanderRadius);
                _agent.SetDestination(target);

                float timeout = 10f;
                float elapsed = 0f;
                while (_state == State.Wandering && elapsed < timeout)
                {
                    elapsed += Time.deltaTime;
                    if (!_agent.pathPending && _agent.remainingDistance < 0.3f)
                        break;
                    yield return null;
                }
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
            if (_state != State.Wandering && _state != State.Howling) continue;

            bool heard = DetectByHearing();
            bool seen = !heard && DetectByVision();

            if (heard || seen)
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
            Ray ray = new Ray(transform.position + Vector3.up * 0.5f, dirToTarget);
            if (Physics.Raycast(ray, dist, _obstacleLayer)) continue;

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
            float playerSpeed = 0f;

            var rb = target.GetComponent<Rigidbody>();
            if (rb != null)
                playerSpeed = rb.linearVelocity.magnitude;
            else
            {
                var cc = target.GetComponent<CharacterController>();
                if (cc != null) playerSpeed = cc.velocity.magnitude;
            }

            if (playerSpeed < Config.FootstepMinSpeed) continue;

            _player = target;
            return true;
        }
        return false;
    }

    private IEnumerator AlertRoutine()
    {
        if (_state != State.Wandering) yield break;
        if (Config == null) yield break;

        _agent.ResetPath();

        if (Random.value < Config.HowlChance)
        {
            _state = State.Howling;
            if (_anim != null) _anim.SetHowling(true);
            yield return new WaitForSeconds(Config.HowlDuration);
            if (_anim != null) _anim.SetHowling(false);
        }

        if (!_isDead) BeginChase();
    }

    private void BeginChase()
    {
        if (Config == null) return;

        _homePosition = transform.position;
        _state = State.Chasing;
        _agent.speed = Config.MoveSpeed;
        _lostTargetTimer = 0f;
    }

    private void UpdateChase()
    {
        if (Config == null) return;
        if (_player == null) { BeginReturn(); return; }

        float dist = Vector3.Distance(transform.position, _player.position);

        if (dist > Config.ChaseRadius)
        {
            BeginReturn();
            return;
        }

        if (dist <= Config.AttackRange)
        {
            TryAttack();
            return;
        }

        _agent.SetDestination(_player.position);

        bool canDetect = DetectByVision() || DetectByHearing();
        if (!canDetect)
        {
            _lostTargetTimer += Time.deltaTime;
            if (_lostTargetTimer >= Config.LoseTargetTime)
                BeginReturn();
        }
        else
        {
            _lostTargetTimer = 0f;
        }
    }

    private void BeginReturn()
    {
        if (Config == null) return;

        _state = State.Returning;
        _agent.speed = Config.WalkSpeed;
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

    private void TryAttack()
    {
        if (Config == null) return;
        if (Time.time < _lastAttackTime + Config.AttackCooldown) return;
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
        if (Config == null) yield break;

        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist <= Config.AttackRange + 0.5f)
        {
            var damageable = _player.GetComponentInParent<IDamageable>();
            if (damageable != null && !damageable.IsDead)
                damageable.TakeDamage(Config.BaseDamage, gameObject);
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

        float despawnDelay = Config != null ? Config.DespawnDelay : 120f;
        ObjectPool.Instance.ReturnDelayed(gameObject, despawnDelay);

        if (_spawnPoint != null)
            _spawnPoint.Invoke("OnWolfDespawned", despawnDelay);
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

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(_homePosition, 0.5f);
    }
}