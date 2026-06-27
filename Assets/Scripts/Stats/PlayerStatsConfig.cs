using UnityEngine;

namespace SimpleSurvival.Stats
{
    [CreateAssetMenu(menuName = "Simple Survival/Stats/Player Stats Config", fileName = "PlayerStatsConfig")]
    public sealed class PlayerStatsConfig : BaseStatsConfig
    {
        [Header("Hunger")]
        [SerializeField] private float maxHunger = 100f;
        [SerializeField] private float startHunger = 100f;
        [SerializeField] private float hungerDecayPerSec = 0.01f;

        [Header("Thirst")]
        [SerializeField] private float maxThirst = 100f;
        [SerializeField] private float startThirst = 100f;
        [SerializeField] private float thirstDecayPerSec = 0.03f;

        [Header("HP Regen (Tick-based)")]
        [Tooltip("HP healed per tick.")]
        [SerializeField] private float hpRegenAmount = 5f;
        [Tooltip("Seconds between each regen tick.")]
        [SerializeField] private float hpRegenInterval = 1f;

        [Header("Starvation Damage (Tick-based)")]
        [SerializeField] private float starveDamageAmount = 5f;
        [SerializeField] private float starveDamageInterval = 1f;

        [Header("Dehydrate Damage (Tick-based)")]
        [SerializeField] private float dehydrateDamageAmount = 5f;
        [SerializeField] private float dehydrateDamageInterval = 1f;

        [Header("Movement")]
        [Tooltip("Hệ số nhân với weight của vũ khí để tính phần trăm giảm tốc. " +
            "Ví dụ 0.02 = mỗi 1 weight giảm 2% tốc độ chạy.")]
        [SerializeField] private float weightSpeedFactor = 0.02f;

        public float MaxHunger => maxHunger;
        public float StartHunger => startHunger;
        public float HungerDecayPerSec => hungerDecayPerSec;

        public float MaxThirst => maxThirst;
        public float StartThirst => startThirst;
        public float ThirstDecayPerSec => thirstDecayPerSec;

        public float HPRegenAmount => hpRegenAmount;
        public float HPRegenInterval => hpRegenInterval;

        public float StarveDamageAmount => starveDamageAmount;
        public float StarveDamageInterval => starveDamageInterval;

        public float DehydrateDamageAmount => dehydrateDamageAmount;
        public float DehydrateDamageInterval => dehydrateDamageInterval;

        public float WeightSpeedFactor => weightSpeedFactor;
    }
}