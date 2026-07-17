using UnityEngine;

namespace SimpleSurvival.Stats
{
    [CreateAssetMenu(menuName = "Simple Survival/Stats/NPC Config", fileName = "NewNPCStatsConfig")]
    public sealed class NPCStatsConfig : BaseStatsConfig
    {
        [Header("Identity")]
        [SerializeField] private string displayName;
        [SerializeField] private Color hpBarColor = Color.green;

        public string DisplayName => displayName;
        public Color HPBarColor => hpBarColor;
    }
}