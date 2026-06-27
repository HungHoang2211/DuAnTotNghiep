using UnityEngine;

namespace SimpleSurvival.Stats
{
    [CreateAssetMenu(menuName = "Simple Survival/Stats/ZombieBoss Config", fileName = "ZombieBossStatsConfig")]
    public sealed class ZombieBossStatsConfig : EnemyStatsConfig
    {
        [Header("Stun (when boss attack hits player)")]
        [SerializeField] private float stunDuration = 3f;

        [Header("Summon")]
        [Tooltip("Các mốc % HP (giảm dần) sẽ kích hoạt triệu hồi minion. Ví dụ: 0.5 = 50% HP, 0.2 = 20% HP.")]
        [SerializeField] private float[] summonHpThresholds = { 0.5f, 0.2f };

        [Header("Laser (chase orb)")]
        [Tooltip("Khi đang đuổi mà không tấn công được, cứ sau bao lâu thì bắn laser (giây)")]
        [SerializeField] private float chaseOrbInterval = 10f;

        public float StunDuration => stunDuration;
        public float[] SummonHpThresholds => summonHpThresholds;
        public float ChaseOrbInterval => chaseOrbInterval;
    }
}