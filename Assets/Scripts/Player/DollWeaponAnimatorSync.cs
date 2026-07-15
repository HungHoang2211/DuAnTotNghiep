using UnityEngine;
using SimpleSurvival.Items;

namespace SimpleSurvival.Player
{
    public sealed class DollWeaponAnimatorSync : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerEquipment playerEquipment;
        [SerializeField] private AnimatorOverrideController defaultOverrideController;

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        private void OnEnable()
        {
            if (playerEquipment != null && playerEquipment.System != null)
                playerEquipment.System.OnSlotChanged += HandleSlotChanged;

            Sync();
        }

        private void OnDisable()
        {
            if (playerEquipment != null && playerEquipment.System != null)
                playerEquipment.System.OnSlotChanged -= HandleSlotChanged;
        }

        private void HandleSlotChanged(EquipSlot slot, int index, ItemStack stack)
        {
            if (slot != EquipSlot.Weapon) return;
            Sync();
        }

        private void Sync()
        {
            if (playerEquipment == null || animator == null) return;

            ItemStack stack = playerEquipment.System.GetSlot(EquipSlot.Weapon, 0);
            AnimatorOverrideController overrideController = ResolveOverrideController(stack);

            if (overrideController == null) return;
            if (animator.runtimeAnimatorController == overrideController) return;

            animator.runtimeAnimatorController = overrideController;
        }

        private AnimatorOverrideController ResolveOverrideController(ItemStack stack)
        {
            if (stack == null) return defaultOverrideController;

            WeaponAbility weapon = stack.ItemData.GetAbility<WeaponAbility>();
            if (weapon != null && weapon.OverrideController != null)
                return weapon.OverrideController;

            return defaultOverrideController;
        }
    }
}