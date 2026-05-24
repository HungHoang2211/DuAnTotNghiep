using UnityEngine;

namespace SimpleSurvival.Items
{
    /// <summary>
    /// Lets an equipped item add extra inventory slots — used by backpacks.
    /// Pairs with an EquipmentAbility(Backpack) on the same item.
    /// </summary>
    [CreateAssetMenu(menuName = "Simple Survival/Abilities/Container", fileName = "ContainerAbility")]
    public sealed class ContainerAbility : ItemAbility
    {
        public const string Name = "Container";

        [SerializeField] private int extraSlots;

        public override string AbilityName => Name;

        public int ExtraSlots => extraSlots;
    }
}
