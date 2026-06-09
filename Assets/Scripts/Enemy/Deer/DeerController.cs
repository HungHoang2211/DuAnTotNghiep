using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using SimpleSurvival.Stats;
using Xyla.Core;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyStats))]
public class DeerController : MonoBehaviour
{
    [Header("Movement Feel")]
    [SerializeField] private float _rotationSpeed = 3f;
    [SerializeField] private float _acceleration = 10f;
    [SerializeField] private float _deceleration = 15f;
    [SerializeField] private float _angularSpeed = 0f;

    private enum State { Wandering, Grazing, Fleeing, Dead }
    private State _state = State.Wandering;

    private NavMeshAgent _agent;
    private DeerAnimatorController _anim;
    private DeerSpawnPoint _spawnPoint;
    private EnemyStats _stats;

    private Coroutine _behaviorCoroutine;
    private bool _isDead = false;
    private float _grazeBlockedUntil = 0f;

    private DeerStatsConfig Config => _stats != null ? _stats.EnemyConfig as DeerStatsConfig : null;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _anim = GetComponent<DeerAnimatorController>();
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

    public void Initialize(DeerSpawnPoint spawnPoint)
    {
        if (Config == null)
        {
            Debug.LogError($"[{name}] DeerStatsConfig missing on EnemyStats", this);
            return;
        }

        _spawnPoint = spawnPoint;
        _isDead = false;
        _state = State.Wandering;
        _grazeBlockedUntil = 0f;

        _agent.isStopped = false;
        _agent.speed = Config.MoveSpeed;
        _agent.acceleration = _acceleration;
        _agent.angularSpeed = _angularSpeed;
        _agent.autoBraking = true;
        _agent.stoppingDistance = 0.2f;
        _agent.updateRotation = false;

        if (_anim != null) { _anim.SetDead(false); _anim.SetGrazing(false); _anim.SetSpeed(0f); }

        var loot = GetComponent<DeerLoot>();
        if (loot != null) loot.SetLootable(false);

        var col = GetComponent<Collider>();
        if (col != null) col.enabled = true;

        if (_behaviorCoroutine != null) StopCoroutine(_behaviorCoroutine);
        _behaviorCoroutine = StartCoroutine(BehaviorRoutine());
    }

    private void HandleDeath() => Die();

    private void HandleDamagedBy(GameObject source)
    {
        if (_isDead || source == null) return;
        StartCoroutine(FleeFrom(source.transform.position));
    }

    private void Update()
    {
        if (_isDead) return;

        SmoothRotation();

        if (_anim != null)
            _anim.SetSpeed(_agent.velocity.magnitude);

        if (_state == State.Wandering || _state == State.Grazing)
            CheckForPlayer();
    }

    private void SmoothRotation()
    {
        if (_agent.velocity.sqrMagnitude < 0.1f) return;

        Vector3 moveDir = _agent.velocity.normalized;
        moveDir.y = 0;

        if (moveDir == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(moveDir);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            _rotationSpeed * Time.deltaTime
        );
    }

    private IEnumerator BehaviorRoutine()
    {
        while (!_isDead)
        {
            if (Config == null) yield break;

            float waitTime = Random.Range(Config.WanderIntervalMin, Config.WanderIntervalMax);
            yield return new WaitForSeconds(waitTime);

            if (_state == State.Fleeing || _state == State.Dead) continue;

            bool canGraze = Time.time >= _grazeBlockedUntil;
            bool willGraze = canGraze && Random.value < Config.GrazeChance;

            if (willGraze)
                yield return StartCoroutine(GrazeRoutine());
            else
                MoveToRandomPoint();
        }
    }

    private IEnumerator GrazeRoutine()
    {
        if (Config == null) yield break;

        _state = State.Grazing;
        _agent.ResetPath();
        if (_anim != null) _anim.SetGrazing(true);

        float grazeDuration = Random.Range(Config.GrazeMinDuration, Config.GrazeMaxDuration);
        float elapsed = 0f;

        while (elapsed < grazeDuration && _state == State.Grazing && !_isDead)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (_anim != null) _anim.SetGrazing(false);
        if (_state == State.Grazing) _state = State.Wandering;
    }

    private void MoveToRandomPoint()
    {
        if (Config == null) return;

        _state = State.Wandering;
        Vector3 target = GetRandomNavMeshPoint(transform.position, Config.WanderRadius);
        _agent.SetDestination(target);
    }

    private Vector3 GetRandomNavMeshPoint(Vector3 origin, float radius)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector2 circle = Random.insideUnitCircle * radius;
            Vector3 candidate = origin + new Vector3(circle.x, 0, circle.y);
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, radius, NavMesh.AllAreas))
                return hit.position;
        }
        return origin;
    }

    private void CheckForPlayer()
    {
        if (Config == null) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, Config.DetectionRadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                StartCoroutine(FleeFrom(hit.transform.position));
                return;
            }
        }
    }

    public void OnPlayerInteract(Vector3 playerPosition)
    {
        if (_isDead) return;
        StartCoroutine(FleeFrom(playerPosition));
    }

    private IEnumerator FleeFrom(Vector3 playerPosition)
    {
        if (Config == null) yield break;
        if (_state == State.Dead || _state == State.Fleeing) yield break;

        if (_anim != null) _anim.SetGrazing(false);

        _state = State.Fleeing;
        _agent.speed = Config.FleeSpeed;

        Vector3 fleeDir = (transform.position - playerPosition).normalized;
        Vector3 fleeTarget = transform.position + fleeDir * Config.FleeDistance;

        if (NavMesh.SamplePosition(fleeTarget, out NavMeshHit hit, Config.FleeDistance, NavMesh.AllAreas))
            _agent.SetDestination(hit.position);

        float timeout = 5f, elapsed = 0f;
        while (_agent.pathPending || _agent.remainingDistance > 0.5f)
        {
            elapsed += Time.deltaTime;
            if (elapsed > timeout) break;
            yield return null;
        }

        _agent.ResetPath();
        _agent.speed = Config.MoveSpeed;
        _state = State.Wandering;

        _grazeBlockedUntil = Time.time + Config.GrazeCooldownAfterFlee;
    }

    public void Die()
    {
        if (_isDead) return;
        _isDead = true;
        _state = State.Dead;

        StopAllCoroutines();
        _agent.isStopped = true;
        _agent.ResetPath();

        if (_anim != null) { _anim.SetGrazing(false); _anim.SetSpeed(0f); _anim.SetDead(true); }

        var loot = GetComponent<DeerLoot>();
        if (loot != null) loot.SetLootable(true);

        float despawnDelay = Config != null ? Config.DespawnDelay : 120f;
        ObjectPool.Instance.ReturnDelayed(gameObject, despawnDelay);

        if (_spawnPoint != null)
            _spawnPoint.Invoke("OnDeerDespawned", despawnDelay);
    }

    private void OnDrawGizmosSelected()
    {
        if (Config == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, Config.DetectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, Config.FleeDistance);
    }
}