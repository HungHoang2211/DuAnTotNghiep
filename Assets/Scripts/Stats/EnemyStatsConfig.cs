using UnityEngine;

namespace SimpleSurvival.Stats
{
    [CreateAssetMenu(menuName = "Simple Survival/Stats/Enemy Config", fileName = "EnemyStatsConfig")]
    public class EnemyStatsConfig : BaseStatsConfig
    {
        public bool IsRunner => isRunner;

        [Header("Identity")]
        [SerializeField] private EnemyKind kind;
        [SerializeField] private string displayName;
        [SerializeField] private Color hpBarColor = Color.red;

        [Header("Detection")]
        [SerializeField] private float visionRange = 12f;
        [SerializeField] private float visionAngle = 100f;
        [SerializeField] private float hearingRadius = 8f;
        [SerializeField] private float hearingNoiseThreshold = 0.3f;

        [Header("Movement (chase = BaseStats.MoveSpeed)")]
        [SerializeField] private float wanderSpeed = 1.5f;
        [SerializeField] private float wanderRadius = 5f;
        [SerializeField] private float wanderIntervalMin = 2f;
        [SerializeField] private float wanderIntervalMax = 5f;

        [Header("Chase")]
        [SerializeField] private float chaseRadius = 20f;
        [SerializeField] private float loseTargetTime = 3f;

        [Header("Combat")]
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private float attackCooldown = 1.5f;

        [Header("Behavior")]
        [SerializeField] private float howlChance = 0.5f;
        [SerializeField] private float howlDuration = 2f;
        [SerializeField] private float despawnDelay = 120f;
        [SerializeField] private bool isRunner = false;

        public EnemyKind Kind => kind;
        public string DisplayName => displayName;
        public Color HPBarColor => hpBarColor;

        public float VisionRange => visionRange;
        public float VisionAngle => visionAngle;
        public float HearingRadius => hearingRadius;
        public float HearingNoiseThreshold => hearingNoiseThreshold;

        public float WanderSpeed => wanderSpeed;
        public float WanderRadius => wanderRadius;
        public float WanderIntervalMin => wanderIntervalMin;
        public float WanderIntervalMax => wanderIntervalMax;

        public float ChaseRadius => chaseRadius;
        public float LoseTargetTime => loseTargetTime;

        public float AttackRange => attackRange;
        public float AttackCooldown => attackCooldown;

        public float HowlChance => howlChance;
        public float HowlDuration => howlDuration;
        public float DespawnDelay => despawnDelay;
    }
}