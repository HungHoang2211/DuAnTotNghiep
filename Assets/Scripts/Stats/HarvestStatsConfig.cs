using UnityEngine;

namespace SimpleSurvival.Stats
{
    [CreateAssetMenu(menuName = "Simple Survival/Stats/Harvest Stats Config", fileName = "HarvestStatsConfig")]
    public sealed class HarvestStatsConfig : ScriptableObject
    {
        [SerializeField] private float maxHP = 100f;

        public float MaxHP => maxHP;
    }
}