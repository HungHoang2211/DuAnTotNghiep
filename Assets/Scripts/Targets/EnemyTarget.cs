using UnityEngine;
using SimpleSurvival.Stats;

namespace SimpleSurvival.Targets
{
    [RequireComponent(typeof(EnemyStats))]
    public class EnemyTarget : TargetableBase
    {
        private EnemyStats _stats;

        public override TargetType Type => TargetType.Enemy;

        protected void Awake()
        {
            _stats = GetComponent<EnemyStats>();
            if (_stats != null)
                _stats.OnDeath += HandleDeath;
        }

        protected override void OnDestroy()
        {
            if (_stats != null)
                _stats.OnDeath -= HandleDeath;

            base.OnDestroy();
        }

        public override bool CanBeTargeted()
        {
            if (!isActiveAndEnabled) return false;
            if (_stats != null && _stats.IsDead) return false;
            return true;
        }

        private void HandleDeath()
        {
            FireOnDestroyed();
        }

        protected override void OnSpawnFromPool()
        {
            base.OnSpawnFromPool();
        }
    }
}