using UnityEngine;

namespace SimpleSurvival.Stats
{
    [CreateAssetMenu(menuName = "Simple Survival/Stats/Deer Config", fileName = "DeerStatsConfig")]
    public sealed class DeerStatsConfig : EnemyStatsConfig
    {
        [Header("Deer Specific")]
        [SerializeField] private float fleeSpeed = 6f;
        [SerializeField] private float fleeDistance = 10f;
        [SerializeField] private float detectionRadius = 6f;

        [Header("Graze Behavior")]
        [SerializeField] private float grazeChance = 0.6f;
        [SerializeField] private float grazeMinDuration = 3f;
        [SerializeField] private float grazeMaxDuration = 7f;
        [SerializeField] private float grazeCooldownAfterFlee = 10f;

        public float FleeSpeed => fleeSpeed;
        public float FleeDistance => fleeDistance;
        public float DetectionRadius => detectionRadius;

        public float GrazeChance => grazeChance;
        public float GrazeMinDuration => grazeMinDuration;
        public float GrazeMaxDuration => grazeMaxDuration;
        public float GrazeCooldownAfterFlee => grazeCooldownAfterFlee;
    }
}