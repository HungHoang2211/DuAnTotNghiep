using UnityEngine;
using UnityEngine.AI;
using SimpleSurvival.Input;
using SimpleSurvival.Stats;

namespace SimpleSurvival.AI
{
    public sealed class ZombieFatController : BaseEnemyController
    {
        [Header("Refs")]
        [SerializeField] private ZombieFatAnimator _fatAnimator;
        [SerializeField] private EnemyCorpseHandler _corpseHandler;

        [Header("Stuck Detection")]
        [SerializeField] private float stuckCheckInterval = 0.8f;
        [SerializeField] private float stuckDistanceThreshold = 0.15f;
        [SerializeField] private float unstuckRadius = 4f;
        [SerializeField] private float unstuckDuration = 1.2f;

        [Header("Hearing")]
        [SerializeField] private float footstepMinSpeed = 0.1f;

        private Vector3 _lastTrackedPosition;
        private float _nextStuckCheckTime;
        private Vector3 _unstuckPoint;
        private float _unstuckUntil;

        protected override void Awake()
        {
            base.Awake();
            _agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
        }

        protected override void OnEnemyInitialized()
        {
            if (_fatAnimator != null) _fatAnimator.ResetForSpawn();
            _lastTrackedPosition = transform.position;
            _nextStuckCheckTime = 0f;
            _unstuckUntil = 0f;
        }

        protected override void BeginChase()
        {
            base.BeginChase();

            foreach (var skill in _skills)
            {
                if (skill is JumpAttackSkill jumpSkill)
                    jumpSkill.PutOnCooldown();
            }
        }

        protected override bool DetectByHearing()
        {
            if (Config == null) return false;

            Collider[] hits = playerLayer == 0
                ? Physics.OverlapSphere(transform.position, Config.HearingRadius)
                : Physics.OverlapSphere(transform.position, Config.HearingRadius, playerLayer);

            foreach (var hit in hits)
            {
                if (!hit.CompareTag("Player")) continue;

                Transform target = hit.transform;

                var inputReader = target.GetComponentInParent<PlayerInputReader>();
                if (inputReader == null) inputReader = target.root.GetComponentInChildren<PlayerInputReader>();
                if (inputReader != null && inputReader.IsSneakHeld) continue;

                float playerSpeed = 0f;
                var cc = target.GetComponentInParent<CharacterController>();
                if (cc != null) playerSpeed = cc.velocity.magnitude;

                if (playerSpeed < footstepMinSpeed) continue;

                _player = target;
                return true;
            }
            return false;
        }

        protected override Vector3 GetChaseDestination()
        {
            if (Time.time < _unstuckUntil)
                return _unstuckPoint;
            return base.GetChaseDestination();
        }

        protected override void UpdateChase()
        {
            if (_fatAnimator != null && _fatAnimator.IsInAttackState)
            {
                _agent.isStopped = true;
                _agent.nextPosition = transform.position;
                if (_player != null) FaceTarget(_player, Config?.RotationSpeed ?? 360f);
                return;
            }

            if (Config == null || _player == null)
            {
                BeginIdle();
                return;
            }

            float dist = Vector3.Distance(transform.position, _player.position);

            if (!_escortMode && dist > Config.ChaseRadius)
            {
                BeginIdle();
                return;
            }

            float engageRange = GetMaxEngageRange();

            if (dist <= engageRange)
            {
                FaceTarget(_player, Config.RotationSpeed);
                TryUseSkill();

                if (_state == EnemyState.Attacking)
                {
                    _agent.isStopped = true;
                    _agent.ResetPath();
                    _agent.nextPosition = transform.position;
                    if (_fatAnimator != null) _fatAnimator.SetMoveSpeed(0f);
                    return;
                }
            }

            _agent.isStopped = false;
            _agent.SetDestination(GetChaseDestination());
            MoveAlongAgentPath(Config.MoveSpeed, Config.RotationSpeed, _escortMode);

            if (_fatAnimator != null)
            {
                float speed = _characterController != null ? _characterController.velocity.magnitude : 0f;
                _fatAnimator.SetMoveSpeed(speed);
            }

            if (!_escortMode)
            {
                if (!CanStillDetect())
                {
                    _lostTargetTimer += Time.deltaTime;
                    if (_lostTargetTimer >= Config.LoseTargetTime)
                        BeginIdle();
                }
                else _lostTargetTimer = 0f;
            }

            CheckStuck();
        }

        private float GetMaxEngageRange()
        {
            float max = Config.AttackRange;
            foreach (var skill in _skills)
            {
                if (skill != null && skill.MaxRange > max)
                    max = skill.MaxRange;
            }
            return max;
        }

        protected override void UpdateAttacking()
        {
            base.UpdateAttacking();

            if (_fatAnimator != null)
                _fatAnimator.SetMoveSpeed(0f);
        }

        private void CheckStuck()
        {
            if (_state != EnemyState.Chasing) return;
            if (Time.time < _unstuckUntil) return;
            if (Time.time < _nextStuckCheckTime) return;

            float moved = Vector3.Distance(transform.position, _lastTrackedPosition);
            Vector3 desiredVel = _agent.desiredVelocity;

            if (moved < stuckDistanceThreshold && desiredVel.sqrMagnitude > 0.01f)
            {
                _unstuckPoint = GetRandomNavMeshPoint(transform.position, unstuckRadius);
                _unstuckUntil = Time.time + unstuckDuration;
            }

            _lastTrackedPosition = transform.position;
            _nextStuckCheckTime = Time.time + stuckCheckInterval;
        }

        public override void NotifySkillComplete()
        {
            base.NotifySkillComplete();
            if (_fatAnimator != null && _player != null)
                _fatAnimator.SetMoveSpeed(Config != null ? Config.MoveSpeed : 1f);
        }

        protected override void BeginIdle()
        {
            base.BeginIdle();
            if (_fatAnimator != null) _fatAnimator.SetIdle();
        }

        protected override void OnDying()
        {
            if (_characterController != null)
                _characterController.enabled = false;

            foreach (var col in GetComponents<Collider>())
            {
                if (col is CharacterController) continue;
                col.enabled = false;
            }

            if (_fatAnimator != null)
            {
                _fatAnimator.SetIdle();
                _fatAnimator.SetRagdollLayer(LayerMask.NameToLayer("Corpse"));
                _fatAnimator.TriggerDeath();
            }

            if (_corpseHandler != null)
                _corpseHandler.SpawnCorpseLoot(Config?.CorpseLootTable);

            float despawnDelay = Config != null ? Config.DespawnDelay : 120f;
            Destroy(gameObject, despawnDelay);
            if (_spawnPoint != null)
                _spawnPoint.NotifyDespawned(despawnDelay);
        }

        protected override void StopAllActions()
        {
            if (_fatAnimator != null)
            {
                _fatAnimator.CancelAttack();
                _fatAnimator.SetIdle();
            }
        }
    }
}