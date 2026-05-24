using UnityEngine;

namespace SimpleSurvival.Items
{
    /// <summary>
    /// Makes an item consumable — eating, drinking, or applying a bandage.
    /// The survival system applies these restore values when the item is used.
    /// A zero value simply restores nothing for that stat.
    /// </summary>
    [CreateAssetMenu(menuName = "Simple Survival/Abilities/Consumable", fileName = "ConsumableAbility")]
    public sealed class ConsumableAbility : ItemAbility
    {
        public const string Name = "Consumable";

        [SerializeField] private float restoreHp;
        [SerializeField] private float restoreHunger;
        [SerializeField] private float restoreThirst;

        public override string AbilityName => Name;

        public float RestoreHp => restoreHp;
        public float RestoreHunger => restoreHunger;
        public float RestoreThirst => restoreThirst;
    }
}
