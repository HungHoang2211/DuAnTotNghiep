using UnityEngine;
using SimpleSurvival.Stats;

namespace SimpleSurvival.AI
{
    public sealed class ZombieBossController : BaseEnemyController
    {
        [Header("Refs")]
        [SerializeField] private ZombieBossAnimator _bossAnimator;
        [SerializeField] private EnemyCorpseHandler _corpseHandler;

        [Header("Combat")]
        [SerializeField] private float cancelAttackBuffer = 1.5f;

        private ZombieBossStatsConfig BossConfig => Config as ZombieBossStatsConfig;

        protected override void OnEnemyInitialized()
        {
            if (_bossAnimator != null) _bossAnimator.ResetForSpawn();
        }

        protected override void UpdateChase()
        {
            base.UpdateChase();

            if (_bossAnimator == null) return;

            float speed = _characterController != null ? _characterController.velocity.magnitude : 0f;
            _bossAnimator.SetMoveSpeed(speed);
        }

        protected override void UpdateAttacking()
        {
            base.UpdateAttacking();

            if (_player == null || Config == null) return;

            float dist = Vector3.Distance(transform.position, _player.position);
            if (dist > Config.AttackRange + cancelAttackBuffer)
                CancelActiveSkill();
        }

        private void CancelActiveSkill()
        {
            foreach (var skill in _skills)
            {
                if (skill != null && skill.IsExecuting)
                    skill.Cancel();
            }

            if (_bossAnimator != null) _bossAnimator.CancelAttack();

            NotifySkillComplete();
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

            var mainCol = GetComponent<Collider>();
            if (mainCol != null) mainCol.enabled = false;

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