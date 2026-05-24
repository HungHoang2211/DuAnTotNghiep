using UnityEngine;

namespace SimpleSurvival.Items
{
    /// <summary>
    /// Makes an item equippable into a specific slot and lets it contribute armor.
    /// A backpack uses this with EquipSlot.Backpack and armorValue 0.
    /// </summary>
    [CreateAssetMenu(menuName = "Simple Survival/Abilities/Equipment", fileName = "EquipmentAbility")]
    public sealed class EquipmentAbility : ItemAbility
    {
        public const string Name = "Equipment";

        [SerializeField] private EquipSlot equipSlot;
        [SerializeField] private float armorValue;

        public override string AbilityName => Name;

        public EquipSlot EquipSlot => equipSlot;
        public float ArmorValue => armorValue;
    }
}
