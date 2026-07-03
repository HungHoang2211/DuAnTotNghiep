using UnityEngine;
using UnityEngine.AI;
using SimpleSurvival.Actions;
using SimpleSurvival.Player;
using SimpleSurvival.Input;
using SimpleSurvival.Targets;
using SimpleSurvival.Stats;
using SimpleSurvival.Combat;

namespace SimpleSurvival.Pets
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(CharacterController))]
    public sealed class DogController : MonoBehaviour
    {
        private enum DogState { Follow, Combat }

        [Header("References")]
        [SerializeField] private PlayerActionController playerActionController;
        [SerializeField] private PlayerInputReader playerInputReader;
        [SerializeField] private PlayerTargetChecker targetChecker;
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private DogAnimator dogAnimator;
        [SerializeField] private DogAttackSkill attackSkill;

        [Tooltip("Điểm neo trên Player để Dog bám theo (vd: DogFollowPoint đặt sau lưng Player). Để trống sẽ dùng thẳng gốc Transform của Player.")]
        [SerializeField] private Transform followPoint;

        [Header("Follow Settings")]
        [SerializeField] private float followDistance = 4f;
        [SerializeField] private float stopDistance = 2f;
        [SerializeField] private float runSpeed = 5f;
        [SerializeField] private float sneakSpeedMultiplier = 0.6f;
        [SerializeField] private float rotationSpeed = 360f;

        [Header("Combat Settings")]
        [SerializeField] private float loseTargetTime = 2f;
        [Tooltip("Sau khi player bị 1 enemy đánh trúng, Dog vẫn coi enemy đó là mối đe doạ trong khoảng thời gian này dù player không tấn công.")]
        [SerializeField] private float playerAttackedGraceTime = 3f;

        [Header("Reaction Delay")]
        [Tooltip("Dog phản ứng chậm hơn Player bao nhiêu giây trước khi bắt đầu đuổi theo hoặc vào combat.")]
        [SerializeField] private float actionDelay = 2f;

        private NavMeshAgent _agent;
        private CharacterController _characterController;
        private DogState _state = DogState.Follow;
        private Transform _combatTarget;
        private Transform _enemyAttacker;
        private float _lastPlayerDamagedTime = -999f;
        private float _lostTargetTimer;
        private bool _isFollowing;
        private float _nextPathUpdateTime;

        private Transform _pendingCombatTarget;
        private float _pendingCombatTimer;

        private const float PathUpdateInterval = 0.2f;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _characterController = GetComponent<CharacterController>();

            _agent.updatePosition = false;
            _agent.updateRotation = false;
            _agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            _agent.nextPosition = transform.position;
        }

        private void OnEnable()
        {
            if (playerStats != null) playerStats.OnDamagedBy += HandlePlayerDamaged;
        }

        private void OnDisable()
        {
            if (playerStats != null) playerStats.OnDamagedBy -= HandlePlayerDamaged;
        }

        private void HandlePlayerDamaged(GameObject attacker)
        {
            if (attacker == null) return;
            _enemyAttacker = attacker.transform;
            _lastPlayerDamagedTime = Time.time;
        }

        private void Update()
        {
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
            bool playerAttacking = playerActionController != null &&
                                    playerActionController.CurrentAction is AttackAction;

            ITargetable target = targetChecker != null ? targetChecker.CurrentEnemy : null;
            bool targetValid = target != null && target.Transform != null && target.CanBeTargeted();

            bool enemyStillAttackingPlayer = _enemyAttacker != null &&
                                              (Time.time - _lastPlayerDamagedTime) <= playerAttackedGraceTime &&
                                              IsAttackerAlive(_enemyAttacker);

            Transform desiredTarget = null;

            if (playerAttacking && targetValid)
                desiredTarget = target.Transform;
            else if (enemyStillAttackingPlayer)
                desiredTarget = _enemyAttacker;

            if (desiredTarget != null)
            {
                if (_state == DogState.Combat && _combatTarget == desiredTarget)
                {
                    _lostTargetTimer = 0f;
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
                _isFollowing = false;
                _lostTargetTimer = 0f;
                _state = DogState.Combat;
                return;
            }

            _pendingCombatTarget = null;
            _pendingCombatTimer = 0f;

            if (_state != DogState.Combat) return;

            _lostTargetTimer += Time.deltaTime;
            if (_lostTargetTimer >= loseTargetTime)
            {
                _combatTarget = null;
                _enemyAttacker = null;
                attackSkill?.Cancel();
                _state = DogState.Follow;
            }
        }

        private bool IsAttackerAlive(Transform attacker)
        {
            IDamageable damageable = attacker.GetComponent<IDamageable>();
            if (damageable == null) damageable = attacker.GetComponentInParent<IDamageable>();
            return damageable == null || !damageable.IsDead;
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
            UpdateAgentDestination(_combatTarget.position);
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

            if (_isFollowing)
            {
                if (dist <= stopDistance)
                {
                    _isFollowing = false;
                    StopMoving();
                    if (dogAnimator != null) dogAnimator.SetIdle();
                    return;
                }
            }
            else
            {
                if (dist < followDistance)
                {
                    StopMoving();
                    if (dogAnimator != null) dogAnimator.SetIdle();
                    return;
                }
                _isFollowing = true;
            }

            bool sneaking = playerInputReader != null && playerInputReader.IsSneakHeld;

            float targetSpeed = runSpeed;
            if (sneaking) targetSpeed *= sneakSpeedMultiplier;

            _agent.isStopped = false;
            UpdateAgentDestination(followTarget.position);
            MoveAlongAgentPath(targetSpeed, rotationSpeed);

            if (dogAnimator != null)
                dogAnimator.SetSpeed(1f);
        }

        private void UpdateAgentDestination(Vector3 destination)
        {
            if (Time.time < _nextPathUpdateTime) return;
            _agent.SetDestination(destination);
            _nextPathUpdateTime = Time.time + PathUpdateInterval;
        }

        private Transform GetFollowTarget()
        {
            return followPoint != null ? followPoint : playerActionController.PlayerTransform;
        }

        public void NotifySkillComplete()
        {
        }

        private void StopMoving()
        {
            _agent.isStopped = true;
            _agent.ResetPath();
            _agent.nextPosition = transform.position;
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
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotSpeed * Time.deltaTime);
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