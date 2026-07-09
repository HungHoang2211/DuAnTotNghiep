using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using SimpleSurvival.Audio;
using SimpleSurvival.Input;
using SimpleSurvival.Stats;

namespace SimpleSurvival.AI
{
    public sealed class ZombieController : BaseEnemyController
    {
        [Header("Refs")]
        [SerializeField] private ZombieAnimator _zombieAnimator;
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

        private PlayerInputReader _playerInputReader;

        protected override void Awake()
        {
            base.Awake();
            _agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
        }

        protected override void OnEnemyInitialized()
        {
            if (_zombieAnimator != null) _zombieAnimator.ResetForSpawn();

            _playerInputReader = null;
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

            if (_player != null) FaceTarget(_player);

            if (_zombieAnimator != null) _zombieAnimator.SetHowling(true);
            yield return new WaitForSeconds(Config.HowlDuration);
            if (_zombieAnimator != null) _zombieAnimator.SetHowling(false);

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
            if (Config == null || _player == null)
            {
                BeginIdle();
                return;
            }

            float dist = Vector3.Distance(transform.position, _player.position);

            if (dist > Config.ChaseRadius)
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
                    if (_zombieAnimator != null) _zombieAnimator.SetLocomotion(false, false);

                    CheckStuck();
                    return;
                }
            }

            _agent.isStopped = false;
            _agent.SetDestination(GetChaseDestination());
            MoveAlongAgentPath(Config.MoveSpeed, Config.RotationSpeed);

            if (_zombieAnimator != null)
            {
                float speed = _characterController.velocity.magnitude;
                bool isMoving = speed > 0.1f;
                bool isRunner = Config != null && Config.IsRunner;
                _zombieAnimator.SetLocomotion(isMoving, isRunner);
            }

            if (!CanStillDetect())
            {
                _lostTargetTimer += Time.deltaTime;
                if (_lostTargetTimer >= Config.LoseTargetTime)
                    BeginIdle();
            }
            else _lostTargetTimer = 0f;

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

            if (_zombieAnimator != null && _player != null)
            {
                bool isRunner = Config != null && Config.IsRunner;
                _zombieAnimator.SetLocomotion(true, isRunner);
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

                if (_playerInputReader == null)
                    _playerInputReader = target.GetComponentInChildren<PlayerInputReader>()
                                       ?? target.GetComponentInParent<PlayerInputReader>();

                if (_playerInputReader != null && _playerInputReader.IsSneakHeld) continue;

                var cc = target.GetComponentInParent<CharacterController>();
                if (cc != null && cc.velocity.magnitude < Config.HearingNoiseThreshold) continue;

                _player = target;
                return true;
            }
            return false;
        }

        protected override void BeginIdle()
        {
            base.BeginIdle();
            if (_zombieAnimator != null)
            {
                _zombieAnimator.SetHowling(false);
                _zombieAnimator.SetIdle();
            }
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

            if (_zombieAnimator != null)
            {
                _zombieAnimator.SetHowling(false);
                _zombieAnimator.SetIdle();
                _zombieAnimator.SetRagdollLayer(LayerMask.NameToLayer("Corpse"));
                _zombieAnimator.TriggerDeath();
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