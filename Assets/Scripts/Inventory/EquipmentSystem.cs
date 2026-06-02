using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.Items
{
    /// <summary>
    /// Manages all equipment slots — equipping, unequipping, and swapping items
    /// with the inventory. Does not know about UI; raises events so EquipmentPanel
    /// can update visuals.
    ///
    /// Each EquipSlot maps to one or more cells (QuickSlot has two cells).
    /// </summary>
    public sealed class EquipmentSystem
    {
        // Each slot holds one ItemStack (or null).
        private readonly Dictionary<EquipSlot, ItemStack[]> _slots;

        /// <summary>Raised when any slot changes. Args: slot, slotIndex, new stack.</summary>
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
                { EquipSlot.QuickSlot, new ItemStack[2] }, // two quick slots
            };
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>Returns the stack in a slot, or null if empty.</summary>
        public ItemStack GetSlot(EquipSlot slot, int index = 0)
        {
            return _slots[slot][index];
        }

        /// <summary>
        /// Tries to equip a stack from inventory into the appropriate slot.
        /// If the slot is occupied, swaps — the old item goes back to inventory.
        /// Returns false if the item cannot be equipped in any slot.
        /// </summary>
        public bool TryEquip(ItemStack stack, InventorySystem inventory,
            int inventoryIndex, EquipSlot targetSlot, int slotIndex = 0)
        {
            if (!CanEquipInSlot(stack, targetSlot))
                return false;

            ItemStack current = _slots[targetSlot][slotIndex];

            // Place new item in equipment slot.
            _slots[targetSlot][slotIndex] = stack;
            inventory.SetSlot(inventoryIndex, current); // null or old item goes back

            OnSlotChanged?.Invoke(targetSlot, slotIndex, stack);
            return true;
        }

        /// <summary>
        /// Auto-equips a stack into the correct slot based on its abilities.
        /// Finds the first matching slot — skips QuickSlot (excluded from auto-equip).
        /// Returns false if no suitable slot found.
        /// </summary>
        public bool TryAutoEquip(ItemStack stack, InventorySystem inventory, int inventoryIndex)
        {
            EquipSlot? slot = GetAutoEquipSlot(stack);
            if (slot == null)
                return false;

            return TryEquip(stack, inventory, inventoryIndex, slot.Value, 0);
        }

        /// <summary>
        /// Unequips the item in a slot and sends it back to inventory.
        /// Returns false if slot is empty or inventory is full.
        /// </summary>
        public bool TryUnequip(EquipSlot slot, int slotIndex, InventorySystem inventory)
        {
            ItemStack current = _slots[slot][slotIndex];
            if (current == null)
                return false;

            int overflow = inventory.AddStack(current);
            if (overflow > 0)
                return false; // inventory full

            _slots[slot][slotIndex] = null;
            OnSlotChanged?.Invoke(slot, slotIndex, null);
            return true;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the auto-equip slot for an item, or null if not auto-equippable.
        /// QuickSlot is intentionally excluded.
        /// </summary>
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

        /// <summary>
        /// Directly sets a slot without going through inventory swap.
        /// Used by drag-drop from equipment to inventory.
        /// </summary>
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