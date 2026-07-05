using System.Collections;
using UnityEngine;
using SimpleSurvival.Stats;

namespace SimpleSurvival.AI
{
    public sealed class ZombieBossController : BaseEnemyController
    {
        [Header("Refs")]
        [SerializeField] private ZombieBossAnimator _bossAnimator;
        [SerializeField] private EnemyCorpseHandler _corpseHandler;

        [Header("Stuck Detection")]
        [SerializeField] private float stuckCheckInterval = 0.8f;
        [SerializeField] private float stuckDistanceThreshold = 0.15f;
        [SerializeField] private float unstuckRadius = 4f;
        [SerializeField] private float unstuckDuration = 1.2f;

        private ZombieBossStatsConfig BossConfig => Config as ZombieBossStatsConfig;

        private Vector3 _lastTrackedPosition;
        private float _nextStuckCheckTime;
        private Vector3 _unstuckPoint;
        private float _unstuckUntil;

        protected override void OnEnemyInitialized()
        {
            if (_bossAnimator != null) _bossAnimator.ResetForSpawn();
            _lastTrackedPosition = transform.position;
            _nextStuckCheckTime = 0f;
            _unstuckUntil = 0f;
        }

        /// <summary>
        /// ZombieBoss không dùng vision cone — detect player bằng proximity (ChaseRadius)
        /// ở mọi state, đảm bảo boss luôn đuổi và tấn công khi player vào tầm.
        /// </summary>
        protected override IEnumerator DetectionRoutine()
        {
            while (!_isDead)
            {
                yield return new WaitForSeconds(0.2f);
                if (_state == EnemyState.Dead) yield break;
                if (_state == EnemyState.Attacking) continue;

                float searchRadius = Config != null ? Config.ChaseRadius : 15f;
                Collider[] hits = playerLayer == 0
                    ? Physics.OverlapSphere(transform.position, searchRadius)
                    : Physics.OverlapSphere(transform.position, searchRadius, playerLayer);

                Transform detected = null;
                foreach (var hit in hits)
                {
                    if (hit.CompareTag("Player")) { detected = hit.transform; break; }
                }

                if (detected != null)
                {
                    _player = detected;
                    if (_state == EnemyState.Idle)
                        OnPlayerDetected();
                }
                else if (_state == EnemyState.Chasing)
                {
                    _player = null;
                }
            }
        }

        protected override void OnPlayerDetected()
        {
            if (Config == null || _player == null)
            {
                BeginChase();
                return;
            }

            float dist = Vector3.Distance(transform.position, _player.position);
            if (dist <= Config.AttackRange)
            {
                BeginChase();
                return;
            }

            StartCoroutine(HowlThenChase());
        }

        private IEnumerator HowlThenChase()
        {
            if (Config == null) yield break;

            _state = EnemyState.Detected;
            _agent.isStopped = true;
            _agent.ResetPath();

            if (_player != null) FaceTarget(_player, Config.RotationSpeed);

            if (_bossAnimator != null) _bossAnimator.TriggerHowl();
            yield return new WaitForSeconds(Config.HowlDuration);

            if (!_isDead) BeginChase();
        }

        protected override Vector3 GetChaseDestination()
        {
            if (Time.time < _unstuckUntil)
                return _unstuckPoint;
            return base.GetChaseDestination();
        }

        protected override void UpdateChase()
        {
            if (_bossAnimator != null && _bossAnimator.IsInAttackState)
            {
                _agent.isStopped = true;
                _agent.nextPosition = transform.position;
                if (_player != null) FaceTarget(_player, Config?.RotationSpeed ?? 360f);
                return;
            }

            base.UpdateChase();

            if (_bossAnimator != null)
            {
                float speed = _characterController != null ? _characterController.velocity.magnitude : 0f;
                _bossAnimator.SetMoveSpeed(speed);
            }

            CheckStuck();
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
            if (_bossAnimator != null && _player != null)
                _bossAnimator.SetMoveSpeed(Config != null ? Config.MoveSpeed : 1f);
        }

        protected override void BeginIdle()
        {
            base.BeginIdle();
            if (_bossAnimator != null) _bossAnimator.SetIdle();
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

            if (_bossAnimator != null)
            {
                _bossAnimator.SetIdle();
                _bossAnimator.SetRagdollLayer(LayerMask.NameToLayer("Corpse"));
                _bossAnimator.TriggerDeath();
            }

            if (_corpseHandler != null)
                _corpseHandler.SpawnCorpseLoot(Config?.CorpseLootTable);

            float despawnDelay = Config != null ? Config.DespawnDelay : 180f;
            Destroy(gameObject, despawnDelay);
            if (_spawnPoint != null)
                _spawnPoint.NotifyDespawned(despawnDelay);
        }
    }
}