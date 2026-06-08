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
        [SerializeField] private float starveDamagePerSec = 1f;

        [Header("Thirst")]
        [SerializeField] private float maxThirst = 100f;
        [SerializeField] private float startThirst = 100f;
        [SerializeField] private float thirstDecayPerSec = 0.03f;
        [SerializeField] private float dehydrateDamagePerSec = 1.5f;

        [Header("HP Regen")]
        [SerializeField] private float hpRegenPerSec = 2f;
        [SerializeField, Range(0f, 1f)] private float regenThreshold = 0.2f;
        [SerializeField] private float hungerCostPerHPRegen = 0.5f;
        [SerializeField] private float thirstCostPerHPRegen = 0.8f;

        public float MaxHunger => maxHunger;
        public float StartHunger => startHunger;
        public float HungerDecayPerSec => hungerDecayPerSec;
        public float StarveDamagePerSec => starveDamagePerSec;

        public float MaxThirst => maxThirst;
        public float StartThirst => startThirst;
        public float ThirstDecayPerSec => thirstDecayPerSec;
        public float DehydrateDamagePerSec => dehydrateDamagePerSec;

        public float HPRegenPerSec => hpRegenPerSec;
        public float RegenThreshold => regenThreshold;
        public float HungerCostPerHPRegen => hungerCostPerHPRegen;
        public float ThirstCostPerHPRegen => thirstCostPerHPRegen;
    }
}