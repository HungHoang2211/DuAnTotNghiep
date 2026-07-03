using UnityEngine;
using SimpleSurvival.Characters.Appearance;

namespace SimpleSurvival.Items
{
    [CreateAssetMenu(menuName = "Simple Survival/Abilities/Container", fileName = "ContainerAbility")]
    public sealed class ContainerAbility : ItemAbility
    {
        public const string Name = "Container";

        [SerializeField] private int extraSlots;

        [Header("Visuals")]
        [Tooltip("Mesh + texture rigid của balo, gán trực tiếp cho BackpackTransform khi equip.")]
        [SerializeField] private BodypartResource backpackResource;

        public override string AbilityName => Name;
        public int ExtraSlots => extraSlots;
        public BodypartResource BackpackResource => backpackResource;
    }
}