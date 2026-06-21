using UnityEngine;

namespace SimpleSurvival.Items
{
    [CreateAssetMenu(menuName = "Simple Survival/Abilities/Container", fileName = "ContainerAbility")]
    public sealed class ContainerAbility : ItemAbility
    {
        public const string Name = "Container";

        [SerializeField] private int extraSlots;

        public override string AbilityName => Name;

        public int ExtraSlots => extraSlots;
    }
}
