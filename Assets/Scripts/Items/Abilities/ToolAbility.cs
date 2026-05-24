using UnityEngine;

namespace SimpleSurvival.Items
{
    /// <summary>
    /// Makes an item usable as a harvesting tool. The interaction system reads
    /// ToolType to decide whether this tool works on a given resource node.
    /// </summary>
    [CreateAssetMenu(menuName = "Simple Survival/Abilities/Tool", fileName = "ToolAbility")]
    public sealed class ToolAbility : ItemAbility
    {
        public const string Name = "Tool";

        [SerializeField] private ToolType toolType;
        [SerializeField] private int bodypartVariantIndex;

        public override string AbilityName => Name;

        public ToolType ToolType => toolType;
        public int BodypartVariantIndex => bodypartVariantIndex;
    }
}
