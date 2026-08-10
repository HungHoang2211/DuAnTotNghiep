using System.Collections;
using UnityEngine;
using SimpleSurvival.Stats;

namespace SimpleSurvival.AI
{
    public sealed class WolfController : BaseEnemyController
    {
        [Header("Refs")]
        [SerializeField] private WolfAnimator _wolfAnimator;
        [SerializeField] private EnemyCorpseHandler _corpseHandler;

        private WolfStatsConfig WolfConfig => Config as WolfStatsConfig;

        protected override void OnEnemyInitialized()
        {
            if (_wolfAnimator != null) _wolfAnimator.ResetForSpawn();
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
                if (_wolfAnimator != null) _wolfAnimator.SetHowling(true);
                yield return new WaitForSeconds(Config.HowlDuration);
                if (_wolfAnimator != null) _wolfAnimator.SetHowling(false);
            }

            if (!_isDead) BeginChase();
        }

        protected override void UpdateChase()
        {
            base.UpdateChase();

            if (_wolfAnimator == null) return;

            float speed = _characterController != null ? _characterController.velocity.magnitude : 0f;
            _wolfAnimator.SetSpeed(speed);
        }

        public override void NotifySkillComplete()
        {
            base.NotifySkillComplete();
            if (_wolfAnimator != null && _player != null)
                _wolfAnimator.SetSpeed(Config != null ? Config.MoveSpeed : 1f);
        }

        protected override bool DetectByHearing()
        {
            if (WolfConfig == null) return false;

            Collider[] hits = playerLayer == 0
                ? Physics.OverlapSphere(transform.position, WolfConfig.HearingRadius)
                : Physics.OverlapSphere(transform.position, WolfConfig.HearingRadius, playerLayer);

            foreach (var hit in hits)
            {
                if (!hit.CompareTag("Player")) continue;

                Transform target = hit.transform;
                float playerSpeed = 0f;

                var cc = target.GetComponentInParent<CharacterController>();
                if (cc != null) playerSpeed = cc.velocity.magnitude;

                if (playerSpeed < WolfConfig.FootstepMinSpeed) continue;

                _player = target;
                return true;
            }
            return false;
        }

        protected override void BeginIdle()
        {
            base.BeginIdle();
            if (_wolfAnimator != null) _wolfAnimator.SetIdle();
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

            if (_wolfAnimator != null)
                _wolfAnimator.TriggerDeath();

            if (_corpseHandler != null)
                _corpseHandler.SpawnCorpseLoot(Config?.CorpseLootTable);

            float despawnDelay = Config != null ? Config.DespawnDelay : 120f;
            Destroy(gameObject, despawnDelay);
            if (_spawnPoint != null)
                _spawnPoint.NotifyDespawned(despawnDelay);
        }

        protected override void OnMapEdgeFreeze()
        {
            if (_wolfAnimator != null) _wolfAnimator.SetIdle();
        }

        protected override void StopAllActions()
        {
            if (_wolfAnimator != null)
            {
                _wolfAnimator.CancelAttack();
                _wolfAnimator.SetHowling(false);
                _wolfAnimator.SetIdle();
            }
        }
    }
}