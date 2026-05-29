using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.Items
{
    /// <summary>
    /// Visual data for one mod attachment point on a weapon.
    /// defaultMesh is what the slot shows when nothing is installed —
    /// for example a plain iron magazine or an empty scope rail.
    /// Leave defaultMesh null if this weapon physically has no such slot.
    /// </summary>
    [Serializable]
    public sealed class WeaponModSlotVisual
    {
        [SerializeField] private WeaponModSlot slot;
        [Tooltip("Mesh shown when this slot has no mod installed. Null = slot does not exist on this weapon.")]
        [SerializeField] private Mesh defaultMesh;
        [SerializeField] private Material defaultMaterial;

        public WeaponModSlot Slot => slot;
        public Mesh DefaultMesh => defaultMesh;
        public Material DefaultMaterial => defaultMaterial;
    }

    /// <summary>
    /// Makes an item usable as a weapon. Combat reads the stats;
    /// the visual system reads the mesh data to swap the character's
    /// Mesh_Weapon SkinnedMeshRenderers on equip.
    /// </summary>
    [CreateAssetMenu(menuName = "Simple Survival/Abilities/Weapon", fileName = "WeaponAbility")]
    public sealed class WeaponAbility : ItemAbility
    {
        public const string Name = "Weapon";

        [Header("Stats - Common")]
        [SerializeField] private float damage;
        [SerializeField] private float attackSpeed;
        [SerializeField] private float range;
        [SerializeField] private float weight;

        [Header("Stats - Ranged Only")]
        [Tooltip("Radius within which nearby enemies are alerted when this weapon fires. 0 = silent (melee).")]
        [SerializeField] private float noise;
        [Tooltip("How much accuracy degrades on sustained fire. 0 = no degradation (melee).")]
        [SerializeField] private float stability;

        [Header("Visuals - Body")]
        [Tooltip("Main weapon mesh shown on the character.")]
        [SerializeField] private Mesh weaponMesh;
        [SerializeField] private Material weaponMaterial;

        [Header("Visuals - Mod Slots")]
        [Tooltip("Define only the slots this weapon physically has. Leave list empty for melee weapons.")]
        [SerializeField] private List<WeaponModSlotVisual> modSlots = new List<WeaponModSlotVisual>();

        public override string AbilityName => Name;

        public float Damage => damage;
        public float AttackSpeed => attackSpeed;
        public float Range => range;
        public float Weight => weight;
        public float Noise => noise;
        public float Stability => stability;
        public Mesh WeaponMesh => weaponMesh;
        public Material WeaponMaterial => weaponMaterial;

        /// <summary>
        /// Returns the default visual for the requested mod slot,
        /// or null if this weapon does not have that slot at all.
        /// </summary>
        public WeaponModSlotVisual GetModSlotVisual(WeaponModSlot slot)
        {
            foreach (WeaponModSlotVisual visual in modSlots)
            {
                if (visual.Slot == slot)
                    return visual;
            }
            return null;
        }

        /// <summary>True when this weapon has the requested mod slot.</summary>
        public bool HasModSlot(WeaponModSlot slot)
        {
            return GetModSlotVisual(slot) != null;
        }
    }
}