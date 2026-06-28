using System.Collections;
using UnityEngine;
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

        private PlayerInputReader _playerInputReader;

        protected override void OnEnemyInitialized()
        {
            if (_zombieAnimator != null) _zombieAnimator.ResetForSpawn();

            _playerInputReader = null;
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

        protected override void UpdateChase()
        {
            base.UpdateChase();

            if (_zombieAnimator == null) return;

            float speed = _characterController.velocity.magnitude;
            bool isMoving = speed > 0.1f;
            bool isRunner = Config != null && Config.IsRunner;
            _zombieAnimator.SetLocomotion(isMoving, isRunner);
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
        }

        protected override void OnDying()
        {
            if (_characterController != null)
                _characterController.enabled = false;

            var mainCol = GetComponent<Collider>();
            if (mainCol != null) mainCol.enabled = false;

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