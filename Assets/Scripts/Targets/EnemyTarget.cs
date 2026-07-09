using UnityEngine;
using SimpleSurvival.Stats;

namespace SimpleSurvival.Targets
{
    public class EnemyTarget : TargetableBase
    {
        private EnemyStats _stats;

        public override TargetType Type => TargetType.Enemy;

        protected void Awake()
        {
            _stats = GetComponentInParent<EnemyStats>();
            if (_stats != null)
                _stats.OnDeath += HandleDeath;
            else
                Debug.LogWarning($"[{name}] EnemyTarget không tìm thấy EnemyStats ở parent", this);
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
            if (_stats != null && _stats.IsInvulnerable) return false;
            return true;
        }

        private void HandleDeath()
        {
            FireOnDestroyed();
        }
    }
}