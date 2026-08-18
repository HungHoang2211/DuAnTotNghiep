using SimpleSurvival.Progression;
using SimpleSurvival.UI.Hud;
using UnityEngine;

namespace SimpleSurvival.Stats
{
    public class EnemyStats : BaseStats
    {
        public EnemyStatsConfig EnemyConfig => baseConfig as EnemyStatsConfig;
        protected override HpHudType HudDamageType => HpHudType.DamageEnemy;

        public override float MaxHP =>
            base.MaxHP * (HardModeSettings.IsActive ? HardModeSettings.HpMultiplier : 1f);

        public override float Armor =>
            base.Armor * (HardModeSettings.IsActive ? HardModeSettings.ArmorMultiplier : 1f);

        public override float MoveSpeed =>
            base.MoveSpeed * (HardModeSettings.IsActive ? HardModeSettings.SpeedMultiplier : 1f);

        protected override void Awake()
        {
            base.Awake();
            if (baseConfig != null && EnemyConfig == null)
            {
                Debug.LogError($"[{name}] EnemyStats requires EnemyStatsConfig, got {baseConfig.GetType().Name}", this);
            }

            if (HardModeSettings.IsActive)
                RestoreHP(MaxHP);

            OnDeath += HandleExpReward;
        }

        private void OnDestroy()
        {
            OnDeath -= HandleExpReward;
        }

        private void HandleExpReward(GameObject source)
        {
            if (EnemyConfig == null || EnemyConfig.ExpReward <= 0) return;
            PlayerLevelSystem.Instance?.AddExperience(EnemyConfig.ExpReward);
        }

        private void OnSpawnFromPool()
        {
            ResetStats();
            if (HardModeSettings.IsActive)
                RestoreHP(MaxHP);
        }
    }
}