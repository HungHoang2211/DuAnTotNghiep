using UnityEngine;

namespace SimpleSurvival.Items
{
    /// <summary>
    /// Makes an item equippable into a specific slot and lets it contribute armor.
    /// Also carries the mesh and material the visual system swaps onto the character's
    /// SkinnedMeshRenderer when this item is equipped.
    /// A backpack uses this with EquipSlot.Backpack and armorValue 0.
    /// </summary>
    [CreateAssetMenu(menuName = "Simple Survival/Abilities/Equipment", fileName = "EquipmentAbility")]
    public sealed class EquipmentAbility : ItemAbility
    {
        public const string Name = "Equipment";

        [Header("Stats")]
        [SerializeField] private EquipSlot equipSlot;
        [SerializeField] private float armorValue;

        [Header("Visuals")]
        [Tooltip("Mesh swapped onto the character's SkinnedMeshRenderer when this item is equipped.")]
        [SerializeField] private Mesh equipMesh;
        [SerializeField] private Material equipMaterial;

        public override string AbilityName => Name;
        public EquipSlot EquipSlot => equipSlot;
        public float ArmorValue => armorValue;
        public Mesh EquipMesh => equipMesh;
        public Material EquipMaterial => equipMaterial;
    }
}