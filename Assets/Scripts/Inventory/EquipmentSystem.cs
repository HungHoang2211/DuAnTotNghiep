using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.Items
{
    public sealed class EquipmentSystem
    {
        private readonly Dictionary<EquipSlot, ItemStack[]> _slots;

        public event Action<EquipSlot, int, ItemStack> OnSlotChanged;

        public EquipmentSystem()
        {
            _slots = new Dictionary<EquipSlot, ItemStack[]>
            {
                { EquipSlot.Weapon,   new ItemStack[1] },
                { EquipSlot.Backpack, new ItemStack[1] },
                { EquipSlot.Helmet,   new ItemStack[1] },
                { EquipSlot.Jacket,   new ItemStack[1] },
                { EquipSlot.Pants,    new ItemStack[1] },
                { EquipSlot.Boots,    new ItemStack[1] },
                { EquipSlot.QuickSlot, new ItemStack[2] },
            };
        }

        public ItemStack GetSlot(EquipSlot slot, int index = 0)
        {
            return _slots[slot][index];
        }

        public bool TryEquip(ItemStack stack, InventorySystem inventory,
            int inventoryIndex, EquipSlot targetSlot, int slotIndex = 0)
        {
            if (!CanEquipInSlot(stack, targetSlot))
                return false;

            ItemStack current = _slots[targetSlot][slotIndex];

            _slots[targetSlot][slotIndex] = stack;
            inventory.SetSlot(inventoryIndex, current);

            OnSlotChanged?.Invoke(targetSlot, slotIndex, stack);
            return true;
        }

        public bool TryAutoEquip(ItemStack stack, InventorySystem inventory, int inventoryIndex)
        {
            EquipSlot? slot = GetAutoEquipSlot(stack);
            if (slot == null)
                return false;

            return TryEquip(stack, inventory, inventoryIndex, slot.Value, 0);
        }

        public bool TryUnequip(EquipSlot slot, int slotIndex, InventorySystem inventory)
        {
            ItemStack current = _slots[slot][slotIndex];
            if (current == null)
                return false;

            int overflow = inventory.AddStack(current);
            if (overflow > 0)
                return false;

            _slots[slot][slotIndex] = null;
            OnSlotChanged?.Invoke(slot, slotIndex, null);
            return true;
        }

        public EquipSlot? GetAutoEquipSlot(ItemStack stack)
        {
            ItemData data = stack.ItemData;

            if (data.HasAbility<WeaponAbility>())
                return EquipSlot.Weapon;

            EquipmentAbility equip = data.GetAbility<EquipmentAbility>();
            if (equip == null)
                return null;

            return equip.EquipSlot switch
            {
                EquipSlot.Helmet => EquipSlot.Helmet,
                EquipSlot.Jacket => EquipSlot.Jacket,
                EquipSlot.Pants => EquipSlot.Pants,
                EquipSlot.Boots => EquipSlot.Boots,
                EquipSlot.Backpack => EquipSlot.Backpack,
                _ => null // QuickSlot excluded
            };
        }

        /// <summary>Returns true when the item can go into the given slot.</summary>
        public bool CanEquipInSlot(ItemStack stack, EquipSlot slot)
        {
            ItemData data = stack.ItemData;

            return slot switch
            {
                EquipSlot.Weapon => data.HasAbility<WeaponAbility>(),
                EquipSlot.Backpack => data.HasAbility<ContainerAbility>(),
                EquipSlot.Helmet => MatchesEquipSlot(data, EquipSlot.Helmet),
                EquipSlot.Jacket => MatchesEquipSlot(data, EquipSlot.Jacket),
                EquipSlot.Pants => MatchesEquipSlot(data, EquipSlot.Pants),
                EquipSlot.Boots => MatchesEquipSlot(data, EquipSlot.Boots),
                EquipSlot.QuickSlot => data.HasAbility<ConsumableAbility>()
                                    || data.HasAbility<WeaponAbility>(),
                _ => false
            };
        }

        public void SetSlotDirect(EquipSlot slot, int slotIndex, ItemStack stack)
        {
            _slots[slot][slotIndex] = stack;
            OnSlotChanged?.Invoke(slot, slotIndex, stack);
        }

        private static bool MatchesEquipSlot(ItemData data, EquipSlot slot)
        {
            EquipmentAbility equip = data.GetAbility<EquipmentAbility>();
            return equip != null && equip.EquipSlot == slot;
        }
    }
}