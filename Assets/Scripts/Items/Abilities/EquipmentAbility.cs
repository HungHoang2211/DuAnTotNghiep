using UnityEngine;
using SimpleSurvival.Characters.Appearance;

namespace SimpleSurvival.Items
{
    [CreateAssetMenu(menuName = "Simple Survival/Abilities/Equipment", fileName = "EquipmentAbility")]
    public sealed class EquipmentAbility : ItemAbility
    {
        public const string Name = "Equipment";

        [Header("Stats")]
        [SerializeField] private EquipSlot equipSlot;
        [SerializeField] private float armorValue;
        [Tooltip("Hệ số tăng tốc độ chạy khi đeo (0-1). Chỉ giày dùng. " +
            "Ví dụ 0.25 = +25% tốc độ. Mặc định 0.")]
        [SerializeField, Range(0f, 1f)] private float speedBonus;

        [Header("Visuals")]
        [Tooltip("Bodypart resource dùng bởi CharacterAppearance khi item này được equip. Null = dùng default resource của slot.")]
        [SerializeField] private BodypartResource appearanceResource;

        public override string AbilityName => Name;
        public EquipSlot EquipSlot => equipSlot;
        public float ArmorValue => armorValue;
        public float SpeedBonus => speedBonus;
        public BodypartResource AppearanceResource => appearanceResource;
    }
}