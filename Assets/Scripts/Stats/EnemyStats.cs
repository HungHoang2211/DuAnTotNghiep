using UnityEngine;
using SimpleSurvival.UI.Hud;
using SimpleSurvival.Progression;

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

            if (FollowNotifyManager.Instance != null)
                FollowNotifyManager.Instance.Notify($"+{EnemyConfig.ExpReward} XP", SpeechHudType.Good);
        }

        private void OnSpawnFromPool()
        {
            ResetStats();
        }
    }
}