using UnityEngine;
using SimpleSurvival.UI.Hud;

namespace SimpleSurvival.Stats
{
    public class EnemyStats : BaseStats
    {
        public EnemyStatsConfig EnemyConfig => baseConfig as EnemyStatsConfig;

        protected override HpHudType HudDamageType => HpHudType.DamageEnemy;

        protected override void Awake()
        {
            base.Awake();
            if (baseConfig != null && EnemyConfig == null)
            {
                Debug.LogError($"[{name}] EnemyStats requires EnemyStatsConfig, got {baseConfig.GetType().Name}", this);
            }
        }

        private void OnSpawnFromPool()
        {
            ResetStats();
        }
    }
}