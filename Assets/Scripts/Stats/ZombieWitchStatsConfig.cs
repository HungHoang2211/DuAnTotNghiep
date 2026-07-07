using UnityEngine;

namespace SimpleSurvival.Stats
{
    [CreateAssetMenu(menuName = "Simple Survival/Stats/Zombie Witch Config", fileName = "ZombieWitchStatsConfig")]
    public class ZombieWitchStatsConfig : EnemyStatsConfig
    {
        [Header("Arm Drop")]
        [Range(0f, 1f)]
        [SerializeField] private float armDropHpThreshold = 0.65f;

        [Header("Summon")]
        [SerializeField] private float[] summonHpThresholds = { 0.5f };

        [Header("Claw Damage")]
        [SerializeField] private float damagePerArm = 20f;
        [SerializeField] private float bothArmsMultiplier = 1.3f;

        public float ArmDropHpThreshold => armDropHpThreshold;
        public float[] SummonHpThresholds => summonHpThresholds;
        public float DamagePerArm => damagePerArm;
        public float BothArmsMultiplier => bothArmsMultiplier;
    }
}