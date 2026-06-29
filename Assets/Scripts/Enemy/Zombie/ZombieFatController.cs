using UnityEngine;
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

        private Vector3 _lastTrackedPosition;
        private float _nextStuckCheckTime;
        private Vector3 _unstuckPoint;
        private float _unstuckUntil;

        protected override void OnEnemyInitialized()
        {
            if (_fatAnimator != null) _fatAnimator.ResetForSpawn();
            _lastTrackedPosition = transform.position;
            _nextStuckCheckTime = 0f;
            _unstuckUntil = 0f;
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

            base.UpdateChase();

            if (_fatAnimator != null)
            {
                float speed = _characterController != null ? _characterController.velocity.magnitude : 0f;
                _fatAnimator.SetMoveSpeed(speed);
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

            var mainCol = GetComponent<Collider>();
            if (mainCol != null) mainCol.enabled = false;

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
    }
}