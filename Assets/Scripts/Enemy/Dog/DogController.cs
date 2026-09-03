using UnityEngine;
using UnityEngine.AI;
using SimpleSurvival.Actions;
using SimpleSurvival.Player;
using SimpleSurvival.Input;
using SimpleSurvival.Targets;
using SimpleSurvival.Stats;
using SimpleSurvival.Combat;
using SimpleSurvival.Quests;

namespace SimpleSurvival.Pets
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(CharacterController))]
    public sealed class DogController : MonoBehaviour
    {
        private enum DogState { Waiting, StandingUp, Follow, Combat, MovingToHouse, SentHome }

        public static DogController Instance { get; private set; }

        [Header("References")]
        [SerializeField] private PlayerActionController playerActionController;
        [SerializeField] private PlayerInputReader playerInputReader;
        [SerializeField] private PlayerTargetChecker targetChecker;
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private DogAnimator dogAnimator;
        [SerializeField] private DogAttackSkill attackSkill;

        [SerializeField] private Transform followPoint;

        [Header("Quest Unlock")]
        [SerializeField] private QuestData unlockQuest;

        [Header("Follow Settings")]
        [SerializeField] private float stopDistance = 2f;
        [SerializeField] private float runSpeed = 5f;
        [SerializeField] private float sneakSpeedMultiplier = 0.6f;
        [SerializeField] private float rotationSpeed = 360f;

        [Header("Combat Settings")]
        [SerializeField] private float playerAttackedGraceTime = 3f;
        [SerializeField] private float playerDealtDamageGraceTime = 3f;
        [SerializeField] private float nearbyEnemyScanRadius = 6f;
        [SerializeField] private LayerMask enemyLayer;

        [Header("Reaction Delay")]
        [SerializeField] private float actionDelay = 2f;

        [Header("Map Transition")]
        [SerializeField] private SimpleSurvival.World.MapLoader mapLoader;

        private NavMeshAgent _agent;
        private CharacterController _characterController;
        private DogState _state = DogState.Follow;
        private Transform _combatTarget;
        private Transform _enemyAttacker;
        private Transform _playerDamagedEnemy;
        private Transform _houseAnchor;
        private float _lastPlayerDamagedTime = -999f;
        private float _lastPlayerDealtDamageTime = -999f;
        private float _nextPathUpdateTime;

        private Transform _pendingCombatTarget;
        private float _pendingCombatTimer;

        private Vector3 _lastStuckCheckPos;
        private float _stuckCheckTimer;
        private float _stuckTimer;
        private int _rerouteAttempts;
        private bool _isDestinationReachable = true;
        private NavMeshPath _scratchPath;

        private const float PathUpdateInterval = 0.2f;
        private const float StuckCheckInterval = 0.5f;
        private const float StuckMoveThreshold = 0.15f;
        private const float StuckTimeToReroute = 0.8f;
        private const float MinStuckTimeToReroute = 0.25f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _agent = GetComponent<NavMeshAgent>();
            _characterController = GetComponent<CharacterController>();

            if (mapLoader == null) mapLoader = SimpleSurvival.World.MapLoader.Instance;
            ResolvePlayerReferences();

            _agent.updatePosition = false;
            _agent.updateRotation = false;
            _agent.autoBraking = false;
            _agent.obstacleAvoidanceType = ObstacleAvoidanceType.GoodQualityObstacleAvoidance;
            _agent.avoidancePriority = 30;
            _agent.nextPosition = transform.position;
            _lastStuckCheckPos = transform.position;
            _scratchPath = new NavMeshPath();

            InitializeQuestState();

            if (_state != DogState.Waiting) PersistAcrossScenes();
        }

        private void PersistAcrossScenes()
        {
            if (Instance != null && Instance != this) return;

            Instance = this;
            transform.SetParent(null, true);
            DontDestroyOnLoad(gameObject);
        }

        private void ResolvePlayerReferences()
        {
            if (playerActionController == null)
                playerActionController = PlayerActionController.Instance;

            if (playerActionController == null) return;

            if (playerInputReader == null) playerInputReader = playerActionController.InputReader;
            if (targetChecker == null) targetChecker = playerActionController.TargetChecker;
            if (playerStats == null) playerStats = playerActionController.PlayerStats;
            if (followPoint == null) followPoint = playerActionController.DogFollowPoint;
        }

        private void InitializeQuestState()
        {
            if (unlockQuest == null) return;

            var manager = QuestManager.Instance;
            bool alreadyDone = manager != null && manager.IsQuestCompleted(unlockQuest);

            if (alreadyDone)
            {
                _state = DogState.Follow;
            }
            else
            {
                _state = DogState.Waiting;
                if (dogAnimator != null) dogAnimator.SetLying(true);
            }
        }

        private void Start()
        {
            if (unlockQuest == null) return;

            var manager = QuestManager.Instance;
            if (manager != null) manager.OnQuestCompleted += HandleQuestCompleted;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;

            var manager = QuestManager.Instance;
            if (manager != null) manager.OnQuestCompleted -= HandleQuestCompleted;
        }

        private void OnEnable()
        {
            if (playerStats != null) playerStats.OnDamagedBy += HandlePlayerDamaged;
            if (playerActionController != null) playerActionController.OnPlayerDealtDamage += HandlePlayerDealtDamage;
            if (dogAnimator != null) dogAnimator.OnStandUpFinished += HandleStandUpFinished;
            if (mapLoader != null) mapLoader.PlayerRepositioned += HandlePlayerRepositioned;
        }

        private void OnDisable()
        {
            if (playerStats != null) playerStats.OnDamagedBy -= HandlePlayerDamaged;
            if (playerActionController != null) playerActionController.OnPlayerDealtDamage -= HandlePlayerDealtDamage;
            if (dogAnimator != null) dogAnimator.OnStandUpFinished -= HandleStandUpFinished;
            if (mapLoader != null) mapLoader.PlayerRepositioned -= HandlePlayerRepositioned;

            var manager = QuestManager.Instance;
            if (manager != null) manager.OnQuestCompleted -= HandleQuestCompleted;
        }

        private void HandlePlayerDamaged(GameObject attacker)
        {
            if (attacker == null) return;
            _enemyAttacker = attacker.transform;
            _lastPlayerDamagedTime = Time.time;
        }

        private void HandlePlayerDealtDamage(GameObject target)
        {
            if (target == null) return;
            _playerDamagedEnemy = target.transform;
            _lastPlayerDealtDamageTime = Time.time;
        }

        private void HandleQuestCompleted(QuestData quest)
        {
            if (unlockQuest == null || quest != unlockQuest) return;
            if (_state != DogState.Waiting) return;

            _state = DogState.StandingUp;
            PersistAcrossScenes();
            if (dogAnimator != null) dogAnimator.TriggerStandUp();
        }

        private void HandleStandUpFinished()
        {
            if (_state != DogState.StandingUp) return;
            _state = DogState.Follow;
        }

        private void HandlePlayerRepositioned()
        {
            if (playerActionController == null) return;
            if (_state == DogState.Waiting || _state == DogState.StandingUp || _state == DogState.SentHome) return;

            Vector3 targetPos = playerActionController.PlayerTransform.position;

            if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                _agent.Warp(hit.position);
                transform.position = hit.position;
            }
            else
            {
                transform.position = targetPos;
            }

            ResetStuckState();
        }

        public void RequestToggleHome(Transform houseAnchor)
        {
            if (_state == DogState.Waiting || _state == DogState.StandingUp) return;

            if (_state == DogState.SentHome)
            {
                _houseAnchor = null;
                _state = DogState.StandingUp;
                if (dogAnimator != null) dogAnimator.TriggerStandUp();
                return;
            }

            if (_state == DogState.MovingToHouse)
            {
                _houseAnchor = null;
                _state = DogState.Follow;
                return;
            }

            if (houseAnchor == null) return;

            _combatTarget = null;
            _enemyAttacker = null;
            attackSkill?.Cancel();

            _houseAnchor = houseAnchor;
            _state = DogState.MovingToHouse;
        }

        private void Update()
        {
            if (_state == DogState.Waiting || _state == DogState.StandingUp || _state == DogState.SentHome) return;

            if (_state == DogState.MovingToHouse)
            {
                UpdateMovingToHouse();
                return;
            }

            UpdateSneakState();
            UpdateCombatTarget();

            if (_state == DogState.Combat) UpdateCombat();
            else UpdateFollow();
        }

        private void UpdateSneakState()
        {
            bool sneaking = playerInputReader != null && playerInputReader.IsSneakHeld;
            if (dogAnimator != null) dogAnimator.SetSneaking(sneaking);
        }

        private void UpdateCombatTarget()
        {
            Transform desiredTarget = ResolveDesiredTarget();

            if (_state == DogState.Combat)
            {
                if (desiredTarget != null && desiredTarget != _combatTarget)
                {
                    _combatTarget = desiredTarget;
                    attackSkill?.Cancel();
                    return;
                }

                if (_combatTarget != null && IsTargetStillValid(_combatTarget))
                    return;

                if (desiredTarget == null)
                    desiredTarget = ScanNearbyEnemy();

                if (desiredTarget != null)
                {
                    _combatTarget = desiredTarget;
                    attackSkill?.Cancel();
                    return;
                }

                _combatTarget = null;
                _enemyAttacker = null;
                attackSkill?.Cancel();
                _state = DogState.Follow;
                return;
            }

            if (desiredTarget == null)
            {
                _pendingCombatTarget = null;
                _pendingCombatTimer = 0f;
                return;
            }

            if (_pendingCombatTarget != desiredTarget)
            {
                _pendingCombatTarget = desiredTarget;
                _pendingCombatTimer = 0f;
            }

            _pendingCombatTimer += Time.deltaTime;
            if (_pendingCombatTimer < actionDelay) return;

            _combatTarget = desiredTarget;
            _pendingCombatTarget = null;
            _state = DogState.Combat;
        }

        private Transform ResolveDesiredTarget()
        {
            bool playerDealtDamageRecently = _playerDamagedEnemy != null &&
                                              (Time.time - _lastPlayerDealtDamageTime) <= playerDealtDamageGraceTime &&
                                              IsTargetStillValid(_playerDamagedEnemy);

            bool enemyStillAttackingPlayer = _enemyAttacker != null &&
                                              (Time.time - _lastPlayerDamagedTime) <= playerAttackedGraceTime &&
                                              IsTargetStillValid(_enemyAttacker);

            if (playerDealtDamageRecently) return _playerDamagedEnemy;
            if (enemyStillAttackingPlayer) return _enemyAttacker;
            return null;
        }

        private Transform ScanNearbyEnemy()
        {
            if (enemyLayer == 0) return null;

            Collider[] hits = Physics.OverlapSphere(transform.position, nearbyEnemyScanRadius, enemyLayer);
            Transform best = null;
            float bestDist = float.MaxValue;

            foreach (var hit in hits)
            {
                if (!IsTargetStillValid(hit.transform)) continue;

                float dist = Vector3.Distance(transform.position, hit.transform.position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = hit.transform;
                }
            }

            return best;
        }

        private bool IsTargetStillValid(Transform target)
        {
            if (target == null) return false;

            IDamageable damageable = target.GetComponent<IDamageable>();
            if (damageable == null) damageable = target.GetComponentInParent<IDamageable>();
            if (damageable != null && damageable.IsDead) return false;

            ITargetable targetable = target.GetComponent<ITargetable>();
            if (targetable == null) targetable = target.GetComponentInParent<ITargetable>();
            if (targetable == null) targetable = target.GetComponentInChildren<ITargetable>();
            if (targetable != null && !targetable.CanBeTargeted()) return false;

            return true;
        }

        private void UpdateCombat()
        {
            if (_combatTarget == null || attackSkill == null)
            {
                _state = DogState.Follow;
                return;
            }

            if (attackSkill.IsExecuting)
            {
                StopMoving();
                FaceTarget(_combatTarget);
                return;
            }

            float dist = Vector3.Distance(transform.position, _combatTarget.position);

            if (dist <= attackSkill.Range)
            {
                StopMoving();
                FaceTarget(_combatTarget);
                if (dogAnimator != null) dogAnimator.SetIdle();
                attackSkill.Execute(_combatTarget);
                return;
            }

            _agent.isStopped = false;
            bool isMoving = UpdateAgentDestination(_combatTarget.position);

            if (!isMoving)
            {
                if (dogAnimator != null) dogAnimator.SetIdle();
                return;
            }

            MoveAlongAgentPath(runSpeed, rotationSpeed);
            if (dogAnimator != null) dogAnimator.SetSpeed(1f);
        }

        private void UpdateFollow()
        {
            if (playerActionController == null)
            {
                if (dogAnimator != null) dogAnimator.SetIdle();
                return;
            }

            Transform followTarget = GetFollowTarget();
            float dist = Vector3.Distance(transform.position, followTarget.position);

            if (dist <= stopDistance)
            {
                StopMoving();
                if (dogAnimator != null) dogAnimator.SetIdle();
                return;
            }

            bool sneaking = playerInputReader != null && playerInputReader.IsSneakHeld;

            float targetSpeed = runSpeed;
            if (sneaking) targetSpeed *= sneakSpeedMultiplier;

            _agent.isStopped = false;
            bool isMoving = UpdateAgentDestination(followTarget.position);

            if (!isMoving)
            {
                if (dogAnimator != null) dogAnimator.SetIdle();
                return;
            }

            MoveAlongAgentPath(targetSpeed, rotationSpeed);

            if (dogAnimator != null)
                dogAnimator.SetSpeed(1f);
        }

        private void UpdateMovingToHouse()
        {
            if (_houseAnchor == null)
            {
                _state = DogState.Follow;
                return;
            }

            float dist = Vector3.Distance(transform.position, _houseAnchor.position);

            if (dist <= stopDistance)
            {
                StopMoving();
                if (dogAnimator != null) dogAnimator.SetLying(true);
                _state = DogState.SentHome;
                return;
            }

            _agent.isStopped = false;
            bool isMoving = UpdateAgentDestination(_houseAnchor.position);

            if (!isMoving)
            {
                if (dogAnimator != null) dogAnimator.SetIdle();
                return;
            }

            MoveAlongAgentPath(runSpeed, rotationSpeed);
            if (dogAnimator != null) dogAnimator.SetSpeed(1f);
        }

        private bool UpdateAgentDestination(Vector3 destination)
        {
            if (!_agent.isOnNavMesh) return false;

            UpdateStuckTimer();

            if (_stuckTimer >= CurrentStuckThreshold())
            {
                Vector3 alt = FindAlternateApproachPoint(destination);
                _agent.SetDestination(alt);
                _nextPathUpdateTime = Time.time + PathUpdateInterval;
                _stuckTimer = 0f;
                _rerouteAttempts++;
                _isDestinationReachable = true;
                return true;
            }

            if (Time.time < _nextPathUpdateTime) return _isDestinationReachable;

            bool reachable = _agent.CalculatePath(destination, _scratchPath) &&
                              _scratchPath.status == NavMeshPathStatus.PathComplete;

            _nextPathUpdateTime = Time.time + PathUpdateInterval;
            _isDestinationReachable = reachable;

            if (!reachable)
            {
                StopMoving();
                return false;
            }

            _agent.SetDestination(destination);
            return true;
        }

        private float CurrentStuckThreshold()
        {
            float t = StuckTimeToReroute * Mathf.Pow(0.5f, _rerouteAttempts);
            return Mathf.Max(t, MinStuckTimeToReroute);
        }

        private void UpdateStuckTimer()
        {
            _stuckCheckTimer += Time.deltaTime;
            if (_stuckCheckTimer < StuckCheckInterval) return;

            float moved = Vector3.Distance(transform.position, _lastStuckCheckPos);
            _lastStuckCheckPos = transform.position;
            _stuckCheckTimer = 0f;

            if (moved < StuckMoveThreshold)
                _stuckTimer += StuckCheckInterval;
            else
            {
                _stuckTimer = 0f;
                _rerouteAttempts = 0;
            }
        }

        private void ResetStuckState()
        {
            _stuckTimer = 0f;
            _stuckCheckTimer = 0f;
            _rerouteAttempts = 0;
            _lastStuckCheckPos = transform.position;
        }

        private static readonly float[] ApproachAngles = { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f };

        private Vector3 FindAlternateApproachPoint(Vector3 destination)
        {
            float angleOffset = Random.Range(0f, 45f);

            foreach (float radius in ApproachRadii)
            {
                foreach (float baseAngle in ApproachAngles)
                {
                    float angle = baseAngle + angleOffset;
                    Vector3 offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * radius;
                    Vector3 candidate = destination + offset;

                    if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
                        continue;

                    if (_agent.CalculatePath(hit.position, _scratchPath) &&
                        _scratchPath.status == NavMeshPathStatus.PathComplete)
                        return hit.position;
                }
            }

            return destination;
        }

        private static readonly float[] ApproachRadii = { 2f, 3.5f };

        private Transform GetFollowTarget()
        {
            return followPoint != null ? followPoint : playerActionController.PlayerTransform;
        }

        public void NotifySkillComplete()
        {
        }

        private void StopMoving()
        {
            if (_agent.isOnNavMesh)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
            }
            _agent.nextPosition = transform.position;
            ResetStuckState();
        }

        private void MoveAlongAgentPath(float moveSpeed, float rotSpeed)
        {
            Vector3 desiredVel = _agent.desiredVelocity;
            Vector3 move = desiredVel.normalized * moveSpeed;
            move.y += Physics.gravity.y * Time.deltaTime;
            _characterController.Move(move * Time.deltaTime);
            _agent.nextPosition = transform.position;

            Vector3 lookDir = new Vector3(desiredVel.x, 0f, desiredVel.z);
            if (lookDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(lookDir), rotSpeed * Time.deltaTime);
                transform.rotation = targetRot;
            }
        }

        private void FaceTarget(Transform target)
        {
            Vector3 dir = target.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), rotationSpeed * Time.deltaTime);
        }
    }
}