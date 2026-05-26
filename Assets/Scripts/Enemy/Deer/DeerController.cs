using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Xyla.Core;

[RequireComponent(typeof(NavMeshAgent))]
public class DeerController : MonoBehaviour
{
    [Header("Wandering")]
    [SerializeField] private float _wanderRadius = 5f;
    [SerializeField] private float _wanderIntervalMin = 3f;
    [SerializeField] private float _wanderIntervalMax = 6f;

    [Header("Grazing")]
    [SerializeField] private float _grazeChance = 0.6f;        // 60% dừng lại ăn cỏ
    [SerializeField] private float _grazeMinDuration = 3f;
    [SerializeField] private float _grazeMaxDuration = 7f;
    [SerializeField] private float _grazeCooldownAfterFlee = 10f; // giây chờ sau khi chạy

    [Header("Flee")]
    [SerializeField] private float _fleeDistance = 10f;
    [SerializeField] private float _fleeSpeed = 6f;
    [SerializeField] private float _normalSpeed = 2f;
    [SerializeField] private float _detectionRadius = 6f;

    [Header("Movement Feel")]
    [SerializeField] private float _rotationSpeed = 3f;   // giảm từ 6 xuống 3
    [SerializeField] private float _acceleration = 10f;
    [SerializeField] private float _deceleration = 15f;
    [SerializeField] private float _angularSpeed = 0f;   // đặt 0 vì updateRotation=false

    [Header("Death")]
    [SerializeField] private float _despawnDelay = 120f;

    private NavMeshAgent _agent;
    private DeerAnimatorController _anim;
    private DeerSpawnPoint _spawnPoint;

    private enum State { Wandering, Grazing, Fleeing, Dead }
    private State _state = State.Wandering;

    private Coroutine _behaviorCoroutine;
    private bool _isDead = false;
    private float _grazeBlockedUntil = 0f; // thời điểm được phép ăn cỏ trở lại

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _anim = GetComponent<DeerAnimatorController>();
    }

    public void Initialize(DeerSpawnPoint spawnPoint)
    {
        _spawnPoint = spawnPoint;
        _isDead = false;
        _state = State.Wandering;
        _grazeBlockedUntil = 0f;

        // Cấu hình NavMeshAgent để không trượt
        _agent.isStopped = false;
        _agent.speed = _normalSpeed;
        _agent.acceleration = _acceleration;
        _agent.angularSpeed = _angularSpeed;
        _agent.autoBraking = true;   // tự phanh khi gần đích
        _agent.stoppingDistance = 0.2f;
        // Tắt rotation của agent — tự xử lý bằng code để smooth hơn
        _agent.updateRotation = false;

        if (_anim != null) { _anim.SetDead(false); _anim.SetGrazing(false); _anim.SetSpeed(0f); }

        var loot = GetComponent<DeerLoot>();
        if (loot != null) loot.SetLootable(false);

        var col = GetComponent<Collider>();
        if (col != null) col.enabled = true;

        if (_behaviorCoroutine != null) StopCoroutine(_behaviorCoroutine);
        _behaviorCoroutine = StartCoroutine(BehaviorRoutine());
    }

    private void OnSpawnFromPool() { }

    private void Update()
    {
        if (_isDead) return;

        // Smooth rotation — quay theo hướng di chuyển từ từ
        SmoothRotation();

        // Cập nhật speed cho Animator — dùng velocity thực của agent
        if (_anim != null)
            _anim.SetSpeed(_agent.velocity.magnitude);

        if (_state == State.Wandering || _state == State.Grazing)
            CheckForPlayer();
    }

    private void SmoothRotation()
    {
        // Chỉ quay khi đang di chuyển đủ nhanh
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

    // ── Behavior Loop ─────────────────────────────────────

    private IEnumerator BehaviorRoutine()
    {
        while (!_isDead)
        {
            float waitTime = Random.Range(_wanderIntervalMin, _wanderIntervalMax);
            yield return new WaitForSeconds(waitTime);

            if (_state == State.Fleeing || _state == State.Dead) continue;

            // Quyết định: đi hay ăn cỏ
            bool canGraze = Time.time >= _grazeBlockedUntil;
            bool willGraze = canGraze && Random.value < _grazeChance;

            if (willGraze)
                yield return StartCoroutine(GrazeRoutine());
            else
                MoveToRandomPoint();
        }
    }

    private IEnumerator GrazeRoutine()
    {
        _state = State.Grazing;
        _agent.ResetPath();
        if (_anim != null) _anim.SetGrazing(true);

        float grazeDuration = Random.Range(_grazeMinDuration, _grazeMaxDuration);
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
        _state = State.Wandering;
        Vector3 target = GetRandomNavMeshPoint(transform.position, _wanderRadius);
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

    // ── Flee ──────────────────────────────────────────────

    private void CheckForPlayer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _detectionRadius);
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
        if (_state == State.Dead || _state == State.Fleeing) yield break;

        // Ngắt ăn cỏ nếu đang ăn
        if (_anim != null) _anim.SetGrazing(false);

        _state = State.Fleeing;
        _agent.speed = _fleeSpeed;

        Vector3 fleeDir = (transform.position - playerPosition).normalized;
        Vector3 fleeTarget = transform.position + fleeDir * _fleeDistance;

        if (NavMesh.SamplePosition(fleeTarget, out NavMeshHit hit, _fleeDistance, NavMesh.AllAreas))
            _agent.SetDestination(hit.position);

        float timeout = 5f, elapsed = 0f;
        while (_agent.pathPending || _agent.remainingDistance > 0.5f)
        {
            elapsed += Time.deltaTime;
            if (elapsed > timeout) break;
            yield return null;
        }

        _agent.ResetPath();
        _agent.speed = _normalSpeed;
        _state = State.Wandering;

        // Chặn ăn cỏ 10 giây sau khi chạy
        _grazeBlockedUntil = Time.time + _grazeCooldownAfterFlee;
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

        if (_anim != null) { _anim.SetGrazing(false); _anim.SetSpeed(0f); _anim.SetDead(true); }

        var loot = GetComponent<DeerLoot>();
        if (loot != null) loot.SetLootable(true);

        ObjectPool.Instance.ReturnDelayed(gameObject, _despawnDelay);

        if (_spawnPoint != null)
            _spawnPoint.Invoke("OnDeerDespawned", _despawnDelay);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _fleeDistance);
    }
}