using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using SimpleSurvival.Stats;
using SimpleSurvival.Audio;
using SimpleSurvival.Input;
using SimpleSurvival.Core;
using SimpleSurvival.Combat;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyStats))]
public class AnimalController : MonoBehaviour, ISpawnableEnemy
{
    public enum BehaviorType { Passive, Predator }

    [Header("Behavior Type")]
    [Tooltip("Set trong Inspector của từng prefab: Passive cho Deer, Predator cho Wolf. " +
             "Trước đây được tự suy ra từ overload Initialize(DeerSpawnPoint)/Initialize(WolfSpawnPoint) — " +
             "nay dùng EnemySpawnPoint chung nên PHẢI set đúng giá trị này thủ công trên prefab.")]
    [SerializeField] private BehaviorType _behaviorType = BehaviorType.Passive;

    [Header("Movement Feel")]
    [HideInInspector] private float _rotationSpeed = 3f;
    [Tooltip("[Predator] Tốc độ xoay khi đang đuổi theo player (cao hơn để bám sát)")]
    [HideInInspector] private float _chaseRotationSpeed = 360f;
    [HideInInspector] private float _acceleration = 10f;
    [Tooltip("[Passive] Không còn được NavMeshAgent dùng trực tiếp trong logic gốc, giữ lại để tương thích Inspector cũ")]
    [HideInInspector] private float _deceleration = 15f;
    [HideInInspector] private float _angularSpeed = 0f;

    [Header("Detection Layers")]
    [SerializeField] private LayerMask _playerLayer;
    [SerializeField] private LayerMask _obstacleLayer;

    private enum State { Wandering, Grazing, Howling, Fleeing, Chasing, Attacking, Returning, Dead }
    private State _state = State.Wandering;

    private NavMeshAgent _agent;
    private AnimalAnimatorController _anim;
    private EnemyStats _stats;
    private EnemyHearing _hearing;

    private IEnemySpawnPoint _spawnPoint;

    private PlayerInputReader _playerInput;

    private Transform _player;

    private Coroutine _behaviorCoroutine;
    private bool _isDead = false;
    private float _grazeBlockedUntil = 0f;
    private float _lastAttackTime = 0f;
    private float _lostTargetTimer = 0f;
    private Vector3 _homePosition;      
    private Vector3 _deathPosition;

    private DeerStatsConfig DeerConfig => _stats != null ? _stats.EnemyConfig as DeerStatsConfig : null;
    private WolfStatsConfig WolfConfig => _stats != null ? _stats.EnemyConfig as WolfStatsConfig : null;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _anim = GetComponent<AnimalAnimatorController>();
        _stats = GetComponent<EnemyStats>();
        _hearing = GetComponent<EnemyHearing>();

        if (_stats == null)
        {
            Debug.LogError($"[{name}] Missing EnemyStats component", this);
            return;
        }

        _stats.OnDeath += HandleDeath;
        _stats.OnDamagedBy += HandleDamagedBy;

        if (_hearing != null)
            _hearing.OnSoundHeard += HandleSoundHeard;
    }

    private void OnDestroy()
    {
        if (_stats != null)
        {
            _stats.OnDeath -= HandleDeath;
            _stats.OnDamagedBy -= HandleDamagedBy;
        }

        if (_hearing != null)
            _hearing.OnSoundHeard -= HandleSoundHeard;
    }

    public void Initialize(IEnemySpawnPoint spawnPoint)
    {
        _spawnPoint = spawnPoint;

        if (_behaviorType == BehaviorType.Passive)
        {
            if (DeerConfig == null)
            {
                Debug.LogError($"[{name}] DeerStatsConfig missing on EnemyStats", this);
                return;
            }

            InitCommon();

            _agent.speed = DeerConfig.MoveSpeed;
            _agent.acceleration = _acceleration;
            _agent.autoBraking = true;
            _agent.stoppingDistance = 0.2f;

            if (_anim != null) _anim.SetGrazing(false);

            if (_behaviorCoroutine != null) StopCoroutine(_behaviorCoroutine);
            _behaviorCoroutine = StartCoroutine(BehaviorRoutine());
        }
        else
        {
            if (WolfConfig == null)
            {
                Debug.LogError($"[{name}] WolfStatsConfig missing on EnemyStats", this);
                return;
            }

            InitCommon();

            _agent.speed = WolfConfig.WalkSpeed;
            _agent.acceleration = _acceleration;
            _agent.autoBraking = false;
            _agent.stoppingDistance = 0f;

            if (_anim != null) _anim.SetHowling(false);

            _player = null;
            _homePosition = transform.position;

            StopAllCoroutines();
            StartCoroutine(WanderRoutine());
            StartCoroutine(DetectionRoutine());
        }
    }

    private void InitCommon()
    {
        _isDead = false;
        _state = State.Wandering;
        _grazeBlockedUntil = 0f;
        _lostTargetTimer = 0f;

        if (_behaviorType == BehaviorType.Passive && _playerInput == null)
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
                _playerInput = player.GetComponentInParent<PlayerInputReader>()
                               ?? player.GetComponent<PlayerInputReader>();
        }

        _agent.isStopped = false;
        _agent.angularSpeed = _angularSpeed;
        _agent.updateRotation = false;

        if (_anim != null) { _anim.SetDead(false); _anim.SetSpeed(0f); }

        var col = GetComponent<Collider>();
        if (col != null) col.enabled = true;
    }

    private void HandleDeath() => Die();

    private void HandleDamagedBy(GameObject source)
    {
        if (_isDead || source == null) return;

        if (_behaviorType == BehaviorType.Passive)
            StartCoroutine(FleeFrom(source.transform.position));
        else
            OnTakeDamage(source.transform);
    }


    private void Update()
    {
        if (_isDead)
        {
            transform.position = _deathPosition;
            return;
        }

        SmoothRotation();

        if (_anim != null)
            _anim.SetSpeed(_agent.velocity.magnitude);

        if (_behaviorType == BehaviorType.Passive)
        {
            if (_state == State.Wandering || _state == State.Grazing)
                CheckForPlayer();
        }
        else
        {
            if (_state == State.Chasing) UpdateChase();
            if (_state == State.Returning) UpdateReturn();
        }
    }

    private void SmoothRotation()
    {
        if (_behaviorType == BehaviorType.Predator)
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

            if (_state == State.Chasing && _player != null)
            {
                Vector3 dirToPlayer = _player.position - transform.position;
                dirToPlayer.y = 0;
                if (dirToPlayer != Vector3.zero)
                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation,
                        Quaternion.LookRotation(dirToPlayer),
                        _chaseRotationSpeed * Time.deltaTime
                    );
                return;
            }

            Vector3 moveDir = _agent.velocity.normalized;
            moveDir.y = 0;
            if (moveDir == Vector3.zero) return;

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.LookRotation(moveDir),
                _rotationSpeed * Time.deltaTime
            );
        }
        else 
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

    private IEnumerator BehaviorRoutine()
    {
        while (!_isDead)
        {
            if (DeerConfig == null) yield break;

            float waitTime = Random.Range(DeerConfig.WanderIntervalMin, DeerConfig.WanderIntervalMax);
            yield return new WaitForSeconds(waitTime);

            if (_state == State.Fleeing || _state == State.Dead) continue;

            bool canGraze = Time.time >= _grazeBlockedUntil;
            bool willGraze = canGraze && Random.value < DeerConfig.GrazeChance;

            if (willGraze)
                yield return StartCoroutine(GrazeRoutine());
            else
                MoveToRandomPoint();
        }
    }

    private IEnumerator GrazeRoutine()
    {
        if (DeerConfig == null) yield break;

        _state = State.Grazing;
        _agent.ResetPath();
        if (_anim != null) _anim.SetGrazing(true);

        float grazeDuration = Random.Range(DeerConfig.GrazeMinDuration, DeerConfig.GrazeMaxDuration);
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
        if (DeerConfig == null) return;

        _state = State.Wandering;
        Vector3 target = GetRandomNavMeshPoint(transform.position, DeerConfig.WanderRadius);
        _agent.SetDestination(target);
    }

    /// <summary>
    /// Vision detection — chỉ kích hoạt khi player KHÔNG sneak.
    /// Khi sneak, deer không thể phát hiện bằng mắt.
    /// </summary>
    private void CheckForPlayer()
    {
        if (DeerConfig == null) return;

        if (_playerInput != null && _playerInput.IsSneakHeld) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, DeerConfig.DetectionRadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                StartCoroutine(FleeFrom(hit.transform.position));
                return;
            }
        }
    }

    private void HandleSoundHeard(SoundEvent soundEvent)
    {
        if (_behaviorType != BehaviorType.Passive) return;
        if (_isDead || _state == State.Dead || _state == State.Fleeing) return;

        bool playerIsSneaking = _playerInput != null && _playerInput.IsSneakHeld;

        switch (soundEvent.Type)
        {
            case SoundType.AttackHit:
            case SoundType.Gunshot:
                StartCoroutine(FleeFrom(soundEvent.Position));
                break;

            case SoundType.GatherHit:
            case SoundType.Footstep:
                if (!playerIsSneaking)
                    StartCoroutine(FleeFrom(soundEvent.Position));
                break;
        }
    }

    public void OnPlayerInteract(Vector3 playerPosition)
    {
        if (_behaviorType != BehaviorType.Passive) return;
        if (_isDead) return;
        StartCoroutine(FleeFrom(playerPosition));
    }

    private IEnumerator FleeFrom(Vector3 playerPosition)
    {
        if (DeerConfig == null) yield break;
        if (_state == State.Dead || _state == State.Fleeing) yield break;

        if (_anim != null) _anim.SetGrazing(false);

        _state = State.Fleeing;
        _agent.speed = DeerConfig.FleeSpeed;

        Vector3 fleeDir = (transform.position - playerPosition).normalized;
        Vector3 fleeTarget = transform.position + fleeDir * DeerConfig.FleeDistance;

        if (NavMesh.SamplePosition(fleeTarget, out NavMeshHit hit, DeerConfig.FleeDistance, NavMesh.AllAreas))
            _agent.SetDestination(hit.position);

        float timeout = 5f, elapsed = 0f;
        while (_agent.pathPending || _agent.remainingDistance > 0.5f)
        {
            elapsed += Time.deltaTime;
            if (elapsed > timeout) break;
            yield return null;
        }

        _agent.ResetPath();
        _agent.speed = DeerConfig.MoveSpeed;
        _state = State.Wandering;

        _grazeBlockedUntil = Time.time + DeerConfig.GrazeCooldownAfterFlee;
    }


    private IEnumerator WanderRoutine()
    {
        while (!_isDead)
        {
            if (WolfConfig == null) yield break;

            if (_state == State.Wandering)
            {
                _homePosition = transform.position;
                Vector3 target = GetRandomNavMeshPoint(transform.position, WolfConfig.WanderRadius);
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

            yield return new WaitForSeconds(Random.Range(WolfConfig.WanderIntervalMin, WolfConfig.WanderIntervalMax));
        }
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
        if (WolfConfig == null) return false;

        Collider[] hits = _playerLayer == 0
            ? Physics.OverlapSphere(transform.position, WolfConfig.VisionRange)
            : Physics.OverlapSphere(transform.position, WolfConfig.VisionRange, _playerLayer);

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;

            Transform target = hit.transform;
            Vector3 dirToTarget = (target.position - transform.position).normalized;

            float angle = Vector3.Angle(transform.forward, dirToTarget);
            if (angle > WolfConfig.VisionAngle * 0.5f) continue;

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
        if (WolfConfig == null) return false;

        Collider[] hits = _playerLayer == 0
            ? Physics.OverlapSphere(transform.position, WolfConfig.HearingRadius)
            : Physics.OverlapSphere(transform.position, WolfConfig.HearingRadius, _playerLayer);

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

            if (playerSpeed < WolfConfig.FootstepMinSpeed) continue;

            _player = target;
            return true;
        }
        return false;
    }

    private IEnumerator AlertRoutine()
    {
        if (_state != State.Wandering) yield break;
        if (WolfConfig == null) yield break;

        _agent.ResetPath();

        if (Random.value < WolfConfig.HowlChance)
        {
            _state = State.Howling;
            if (_anim != null) _anim.SetHowling(true);
            yield return new WaitForSeconds(WolfConfig.HowlDuration);
            if (_anim != null) _anim.SetHowling(false);
        }

        if (!_isDead) BeginChase();
    }

    private void BeginChase()
    {
        if (WolfConfig == null) return;

        _homePosition = transform.position;
        _state = State.Chasing;
        _agent.speed = WolfConfig.MoveSpeed;
        _lostTargetTimer = 0f;
    }

    private void UpdateChase()
    {
        if (WolfConfig == null) return;
        if (_player == null) { BeginReturn(); return; }

        float dist = Vector3.Distance(transform.position, _player.position);

        if (dist > WolfConfig.ChaseRadius)
        {
            BeginReturn();
            return;
        }

        if (dist <= WolfConfig.AttackRange)
        {
            TryAttack();
            return;
        }

        _agent.SetDestination(_player.position);

        bool canDetect = DetectByVision() || DetectByHearing();
        if (!canDetect)
        {
            _lostTargetTimer += Time.deltaTime;
            if (_lostTargetTimer >= WolfConfig.LoseTargetTime)
                BeginReturn();
        }
        else
        {
            _lostTargetTimer = 0f;
        }
    }

    private void BeginReturn()
    {
        if (WolfConfig == null) return;

        _state = State.Returning;
        _agent.speed = WolfConfig.WalkSpeed;
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
        if (WolfConfig == null) return;
        if (Time.time < _lastAttackTime + WolfConfig.AttackCooldown) return;
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
        if (WolfConfig == null) yield break;

        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist <= WolfConfig.AttackRange + 0.5f)
        {
            var damageable = _player.GetComponentInParent<IDamageable>();
            if (damageable != null && !damageable.IsDead)
                damageable.TakeDamage(WolfConfig.BaseDamage, gameObject);
        }

        yield return new WaitForSeconds(0.5f);
        if (!_isDead) BeginChase();
    }

    public void OnTakeDamage(Transform attacker)
    {
        if (_behaviorType != BehaviorType.Predator) return;
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
        _agent.enabled = false;

        _deathPosition = transform.position;

        float despawnDelay;

        if (_behaviorType == BehaviorType.Passive)
        {
            if (_anim != null) { _anim.SetGrazing(false); _anim.SetSpeed(0f); _anim.SetDead(true); }

            despawnDelay = DeerConfig != null ? DeerConfig.DespawnDelay : 120f;
        }
        else
        {
            if (_anim != null) { _anim.SetSpeed(0f); _anim.SetDead(true); _anim.SetHowling(false); }

            despawnDelay = WolfConfig != null ? WolfConfig.DespawnDelay : 120f;
        }

        foreach (var c in GetComponentsInChildren<Collider>())
            c.enabled = false;

        Destroy(gameObject, despawnDelay);

        if (_spawnPoint != null)
            _spawnPoint.NotifyDespawned(despawnDelay);
    }

    private void OnDrawGizmosSelected()
    {
        if (_behaviorType == BehaviorType.Passive)
        {
            if (DeerConfig == null) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, DeerConfig.DetectionRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, DeerConfig.FleeDistance);
        }
        else
        {
            if (WolfConfig == null) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, WolfConfig.HearingRadius);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, WolfConfig.VisionRange);

            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawWireSphere(transform.position, WolfConfig.ChaseRadius);

            Gizmos.color = Color.cyan;
            float half = WolfConfig.VisionAngle * 0.5f;
            Gizmos.DrawRay(transform.position,
                Quaternion.Euler(0, -half, 0) * transform.forward * WolfConfig.VisionRange);
            Gizmos.DrawRay(transform.position,
                Quaternion.Euler(0, half, 0) * transform.forward * WolfConfig.VisionRange);

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_homePosition, 0.5f);
        }
    }
}