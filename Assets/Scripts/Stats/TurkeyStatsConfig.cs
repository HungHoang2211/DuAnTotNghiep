using UnityEngine;

namespace SimpleSurvival.Stats
{
    [CreateAssetMenu(menuName = "Simple Survival/Stats/Turkey Config", fileName = "TurkeyStatsConfig")]
    public sealed class TurkeyStatsConfig : EnemyStatsConfig
    {
        [Header("Turkey Specific")]
        [SerializeField] private float fleeSpeed = 6f;
        [SerializeField] private float fleeDistance = 10f;
        [SerializeField] private float detectionRadius = 6f;

        [Header("Eat Behavior")]
        [SerializeField] private float eatChance = 0.6f;
        [SerializeField] private float eatMinDuration = 3f;
        [SerializeField] private float eatMaxDuration = 7f;
        [SerializeField] private float eatCooldownAfterFlee = 10f;

        public float FleeSpeed => fleeSpeed;
        public float FleeDistance => fleeDistance;
        public float DetectionRadius => detectionRadius;

        public float EatChance => eatChance;
        public float EatMinDuration => eatMinDuration;
        public float EatMaxDuration => eatMaxDuration;
        public float EatCooldownAfterFlee => eatCooldownAfterFlee;
    }
}