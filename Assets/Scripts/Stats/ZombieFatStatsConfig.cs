using UnityEngine;

namespace SimpleSurvival.Stats
{
    [CreateAssetMenu(menuName = "Simple Survival/Stats/ZombieFat Config", fileName = "ZombieFatStatsConfig")]
    public sealed class ZombieFatStatsConfig : EnemyStatsConfig
    {
        [Header("Special Attack (Acid)")]
        [SerializeField] private float specialCooldown = 10f;
        [SerializeField] private float specialRange = 8f;
        [SerializeField] private float specialDamage = 30f;
        [SerializeField] private float acidSpeed = 7f;
        [SerializeField] private float acidLifetime = 3f;

        public float SpecialCooldown => specialCooldown;
        public float SpecialRange => specialRange;
        public float SpecialDamage => specialDamage;
        public float AcidSpeed => acidSpeed;
        public float AcidLifetime => acidLifetime;
    }
}