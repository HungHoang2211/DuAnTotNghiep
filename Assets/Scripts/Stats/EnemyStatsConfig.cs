using UnityEngine;
using SimpleSurvival.Loot;

namespace SimpleSurvival.Stats
{
    [CreateAssetMenu(menuName = "Simple Survival/Stats/Enemy Config", fileName = "EnemyStatsConfig")]
    public class EnemyStatsConfig : BaseStatsConfig
    {
        [Header("Identity")]
        [SerializeField] private EnemyKind kind;
        [SerializeField] private string displayName;
        [SerializeField] private Color hpBarColor = Color.red;

        [Header("Detection")]
        [SerializeField] private float visionRange = 12f;
        [SerializeField] private float visionAngle = 100f;
        [SerializeField] private float hearingRadius = 8f;
        [SerializeField] private float hearingNoiseThreshold = 0.3f;

        [Header("Chase")]
        [SerializeField] private float chaseRadius = 20f;
        [SerializeField] private float loseTargetTime = 3f;

        [Header("Movement")]
        [SerializeField] private float rotationSpeed = 360f;

        [Header("Combat")]
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private float attackCooldown = 1.5f;

        [Header("Behavior")]
        [SerializeField] private float howlChance = 0.5f;
        [SerializeField] private float howlDuration = 2f;
        [SerializeField] private float despawnDelay = 120f;
        [SerializeField] private bool isRunner = false;

        [Header("Wander")]
        [SerializeField] private float wanderSpeed = 1.5f;
        [SerializeField] private float wanderRadius = 5f;
        [SerializeField] private float wanderIntervalMin = 2f;
        [SerializeField] private float wanderIntervalMax = 5f;

        [Header("Corpse Loot")]
        [Tooltip("Loot table khi enemy chết. Để null = không drop gì = xác không tương tác được.")]
        [SerializeField] private LootTable corpseLootTable;

        public EnemyKind Kind => kind;
        public string DisplayName => displayName;
        public Color HPBarColor => hpBarColor;
        public float VisionRange => visionRange;
        public float VisionAngle => visionAngle;
        public float HearingRadius => hearingRadius;
        public float HearingNoiseThreshold => hearingNoiseThreshold;
        public float ChaseRadius => chaseRadius;
        public float LoseTargetTime => loseTargetTime;
        public float RotationSpeed => rotationSpeed;
        public float AttackRange => attackRange;
        public float AttackCooldown => attackCooldown;
        public float HowlChance => howlChance;
        public float HowlDuration => howlDuration;
        public float DespawnDelay => despawnDelay;
        public bool IsRunner => isRunner;
        public float WanderSpeed => wanderSpeed;
        public float WanderRadius => wanderRadius;
        public float WanderIntervalMin => wanderIntervalMin;
        public float WanderIntervalMax => wanderIntervalMax;
        public LootTable CorpseLootTable => corpseLootTable;
    }
}