using UnityEngine;

namespace SimpleSurvival.Items
{
    [CreateAssetMenu(menuName = "Simple Survival/Abilities/Tool", fileName = "ToolAbility")]
    public sealed class ToolAbility : ItemAbility
    {
        public const string Name = "Tool";

        [SerializeField] private ToolType toolType;
        [SerializeField] private float damage = 25f;
        [SerializeField] private AnimatorOverrideController overrideController;

        public override string AbilityName => Name;
        public ToolType ToolType => toolType;
        public float Damage => damage;
        public AnimatorOverrideController OverrideController => overrideController;
    }
}