using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using SimpleSurvival.Stats;

namespace SimpleSurvival.AI
{
    public sealed class BearController : BaseEnemyController
    {
        [Header("Refs")]
        [SerializeField] private BearAnimator _bearAnimator;
        [SerializeField] private EnemyCorpseHandler _corpseHandler;

        [Header("Obstacle Avoidance")]
        [SerializeField] private float stuckCheckInterval = 0.5f;
        [SerializeField] private float stuckDistanceThreshold = 0.1f;
        [SerializeField] private float stuckTimeThreshold = 1f;
        [SerializeField] private float detourRadius = 2.5f;
        [SerializeField] private float detourReachedDistance = 0.5f;

        private Vector3 _lastCheckedPosition;
        private float _stuckCheckTimer;
        private float _stuckTimer;
        private bool _detourActive;
        private Vector3 _detourTarget;

        private BearStatsConfig BearConfig => Config as BearStatsConfig;

        protected override void Awake()
        {
            base.Awake();
            _agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
        }

        protected override void OnEnemyInitialized()
        {
            if (_bearAnimator != null) _bearAnimator.ResetForSpawn();
            _detourActive = false;
            _stuckTimer = 0f;
            _stuckCheckTimer = 0f;
            _lastCheckedPosition = transform.position;
        }

        protected override void OnPlayerDetected()
        {
            StartCoroutine(AlertThenChase());
        }

        private IEnumerator AlertThenChase()
        {
            if (Config == null) yield break;

            _state = EnemyState.Detected;
            _agent.isStopped = true;
            _agent.ResetPath();

            if (_player != null) FaceTarget(_player, Config.RotationSpeed);

            if (Random.value < Config.HowlChance)
            {
                if (_bearAnimator != null) _bearAnimator.SetHowling(true);
                yield return new WaitForSeconds(Config.HowlDuration);
                if (_bearAnimator != null) _bearAnimator.SetHowling(false);
            }

            if (!_isDead) BeginChase();
        }

        protected override Vector3 GetChaseDestination()
        {
            if (_detourActive)
            {
                if (Vector3.Distance(transform.position, _detourTarget) > detourReachedDistance)
                    return _detourTarget;

                _detourActive = false;
            }
            return base.GetChaseDestination();
        }

        protected override void UpdateChase()
        {
            base.UpdateChase();

            if (_bearAnimator != null)
            {
                float speed = _characterController != null ? _characterController.velocity.magnitude : 0f;
                _bearAnimator.SetSpeed(speed);
            }

            CheckStuck();
        }

        private void CheckStuck()
        {
            _stuckCheckTimer += Time.deltaTime;
            if (_stuckCheckTimer < stuckCheckInterval) return;

            float moved = Vector3.Distance(transform.position, _lastCheckedPosition);
            _lastCheckedPosition = transform.position;
            _stuckCheckTimer = 0f;

            if (_agent.isStopped || _player == null)
            {
                _stuckTimer = 0f;
                return;
            }

            if (moved < stuckDistanceThreshold)
            {
                _stuckTimer += stuckCheckInterval;
                if (_stuckTimer >= stuckTimeThreshold)
                {
                    _detourTarget = GetRandomNavMeshPoint(transform.position, detourRadius);
                    _detourActive = true;
                    _stuckTimer = 0f;
                }
            }
            else
            {
                _stuckTimer = 0f;
            }
        }

        public override void NotifySkillComplete()
        {
            base.NotifySkillComplete();
            if (_bearAnimator != null && _player != null)
                _bearAnimator.SetSpeed(Config != null ? Config.MoveSpeed : 1f);
        }

        protected override bool DetectByHearing()
        {
            if (BearConfig == null) return false;

            Collider[] hits = playerLayer == 0
                ? Physics.OverlapSphere(transform.position, BearConfig.HearingRadius)
                : Physics.OverlapSphere(transform.position, BearConfig.HearingRadius, playerLayer);

            foreach (var hit in hits)
            {
                if (!hit.CompareTag("Player")) continue;

                Transform target = hit.transform;
                float playerSpeed = 0f;

                var cc = target.GetComponentInParent<CharacterController>();
                if (cc != null) playerSpeed = cc.velocity.magnitude;

                if (playerSpeed < BearConfig.FootstepMinSpeed) continue;

                _player = target;
                return true;
            }
            return false;
        }

        protected override void BeginIdle()
        {
            base.BeginIdle();
            if (_bearAnimator != null) _bearAnimator.SetIdle();
            _detourActive = false;
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

            if (_bearAnimator != null)
            {
                _bearAnimator.SetRagdollLayer(LayerMask.NameToLayer("Corpse"));
                _bearAnimator.TriggerDeath();
            }

            if (_corpseHandler != null)
                _corpseHandler.SpawnCorpseLoot(Config?.CorpseLootTable);

            float despawnDelay = Config != null ? Config.DespawnDelay : 120f;
            Destroy(gameObject, despawnDelay);
            if (_spawnPoint != null)
                _spawnPoint.NotifyDespawned(despawnDelay);
        }

        protected override void OnMapEdgeFreeze()
        {
            if (_bearAnimator != null) _bearAnimator.SetIdle();
        }

        protected override void StopAllActions()
        {
            if (_bearAnimator != null)
            {
                _bearAnimator.CancelAttack();
                _bearAnimator.CancelSpecialAttack();
                _bearAnimator.SetHowling(false);
                _bearAnimator.SetIdle();
            }
        }
    }
}