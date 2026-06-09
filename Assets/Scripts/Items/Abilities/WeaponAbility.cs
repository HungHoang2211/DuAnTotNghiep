using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.Items
{
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

        [Header("Animation")]
        [SerializeField] private AnimatorOverrideController overrideController;
        [Tooltip("0 = 1 swing only (pistol/rifle). 1 = 2-swing combo (2H). 2 = 3-swing combo (1H melee). 3 = 4-swing combo (fists).")]
        [SerializeField] private int maxComboIndex;
        [SerializeField] private WeaponCategory category;

        [Header("Visuals - Body")]
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
        public AnimatorOverrideController OverrideController => overrideController;
        public int MaxComboIndex => maxComboIndex;
        public WeaponCategory Category => category;
        public Mesh WeaponMesh => weaponMesh;
        public Material WeaponMaterial => weaponMaterial;

        public WeaponModSlotVisual GetModSlotVisual(WeaponModSlot slot)
        {
            foreach (WeaponModSlotVisual visual in modSlots)
            {
                if (visual.Slot == slot)
                    return visual;
            }
            return null;
        }

        public bool HasModSlot(WeaponModSlot slot)
        {
            return GetModSlotVisual(slot) != null;
        }
    }
}