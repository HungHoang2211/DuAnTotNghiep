using UnityEngine;
using SimpleSurvival.Stats;

namespace SimpleSurvival.AI
{
    public sealed class ZombieWitchController : BaseEnemyController
    {
        [Header("Refs")]
        [SerializeField] private ZombieWitchAnimator _witchAnimator;
        [SerializeField] private BodyPartDetacher _armDetacher;
        [SerializeField] private EnemyCorpseHandler _corpseHandler;

        [HideInInspector] private float stuckCheckInterval = 0.8f;
        [HideInInspector] private float stuckDistanceThreshold = 0.15f;
        [HideInInspector] private float unstuckRadius = 4f;
        [HideInInspector] private float unstuckDuration = 1.2f;
        [HideInInspector] private float unstuckMoveSpeedRatio = 0.5f;

        private ZombieWitchStatsConfig WitchConfig => Config as ZombieWitchStatsConfig;

        private Vector3 _lastTrackedPosition;
        private float _nextStuckCheckTime;
        private Vector3 _unstuckPoint;
        private float _unstuckUntil;
        private bool _armDropTriggered;

        public bool HasDroppedArm { get; private set; }
        public int DroppedArmIndex { get; private set; } = -1;

        protected override void OnEnemyInitialized()
        {
            if (_witchAnimator != null) _witchAnimator.ResetForSpawn();
            if (_armDetacher != null) _armDetacher.ResetForSpawn();

            HasDroppedArm = false;
            DroppedArmIndex = -1;
            _armDropTriggered = false;
            _lastTrackedPosition = transform.position;
            _nextStuckCheckTime = 0f;
            _unstuckUntil = 0f;

            if (_stats != null) _stats.OnHPChanged += HandleHPChanged;

            // Witch luôn aggro thẳng vào Player ngay khi vừa được spawn/triệu hồi,
            // không cần chờ DetectionRoutine phát hiện qua tầm nhìn/nghe như enemy thường.
            if (_playerStats != null && !_playerDead)
            {
                _player = _playerStats.transform;
                BeginChase();
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_stats != null) _stats.OnHPChanged -= HandleHPChanged;
        }

        private void HandleHPChanged(float currentHP, float maxHP)
        {
            if (_armDropTriggered || _isDead || maxHP <= 0f) return;

            float threshold = WitchConfig != null ? WitchConfig.ArmDropHpThreshold : 0.65f;
            if (currentHP / maxHP > threshold) return;

            _armDropTriggered = true;
            DropRandomArm();
        }

        private void DropRandomArm()
        {
            if (_armDetacher == null) return;

            int index = _armDetacher.DetachRandom();
            if (index < 0) return;

            DroppedArmIndex = index;
            HasDroppedArm = true;
            if (_witchAnimator != null) _witchAnimator.SetHasDroppedArm(true);
        }

        protected override Vector3 GetChaseDestination()
        {
            if (Time.time < _unstuckUntil) return _unstuckPoint;
            return base.GetChaseDestination();
        }

        /// <summary>
        /// Override toàn bộ (không gọi base.UpdateChase()) để Witch đuổi theo Player
        /// bất kể khoảng cách (bỏ qua Config.ChaseRadius) và không bao giờ "mất dấu"
        /// rồi về Idle (bỏ qua LoseTargetTime) — đúng yêu cầu đuổi theo tấn công bất cứ đâu.
        /// </summary>
        protected override void UpdateChase()
        {
            if (Config == null || _player == null)
            {
                BeginIdle();
                return;
            }

            float dist = Vector3.Distance(transform.position, _player.position);

            if (dist <= Config.AttackRange)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
                _agent.nextPosition = transform.position;
                FaceTarget(_player, Config.RotationSpeed);
                TryUseSkill();
            }
            else
            {
                _agent.isStopped = false;
                _agent.SetDestination(GetChaseDestination());
                MoveAlongAgentPath(Config.MoveSpeed, Config.RotationSpeed);
            }

            if (_witchAnimator != null)
            {
                float speed = _agent.isStopped
                    ? 0f
                    : (_characterController != null ? _characterController.velocity.magnitude : 0f);

                if (Time.time < _unstuckUntil) speed *= unstuckMoveSpeedRatio;
                _witchAnimator.SetMoveSpeed(speed);
            }

            CheckStuck();
        }

        protected override void UpdateRetreat()
        {
            base.UpdateRetreat();

            if (_witchAnimator != null)
            {
                float speed = _agent.isStopped
                    ? 0f
                    : (_characterController != null ? _characterController.velocity.magnitude : 0f);
                _witchAnimator.SetMoveSpeed(speed);
            }
        }

        protected override void EnterHidden()
        {
            if (_witchAnimator != null) _witchAnimator.SetIdle();
            base.EnterHidden();
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

        protected override void BeginIdle()
        {
            base.BeginIdle();
            if (_witchAnimator != null) _witchAnimator.SetIdle();
        }

        public override void NotifySkillComplete()
        {
            base.NotifySkillComplete();
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

            if (_witchAnimator != null)
            {
                _witchAnimator.SetIdle();
                _witchAnimator.SetRagdollLayer(LayerMask.NameToLayer("Corpse"));
                _witchAnimator.TriggerDeath();
            }

            if (_corpseHandler != null)
                _corpseHandler.SpawnCorpseLoot(Config?.CorpseLootTable);

            float despawnDelay = Config != null ? Config.DespawnDelay : 180f;
            Destroy(gameObject, despawnDelay);
            if (_spawnPoint != null)
                _spawnPoint.NotifyDespawned(despawnDelay);
        }
        protected override void OnMapEdgeFreeze()
        {
            if (_witchAnimator != null) _witchAnimator.SetIdle();
        }
        protected override void StopAllActions()
        {
            if (_witchAnimator != null)
            {
                _witchAnimator.CancelAttack();
                _witchAnimator.SetIdle();
            }
        }
    }
}