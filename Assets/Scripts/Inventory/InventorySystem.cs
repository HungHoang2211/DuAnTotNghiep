using System;
using System.Collections.Generic;

namespace SimpleSurvival.Items
{
    /// <summary>
    /// The data layer of an inventory: a fixed list of slots, each holding one
    /// ItemStack or null (empty). Knows nothing about UI — it only stores items
    /// and raises an event when its contents change. The UI listens to that
    /// event and redraws itself.
    /// </summary>
    public sealed class InventorySystem
    {
        private readonly ItemStack[] slots;

        /// <summary>Raised whenever the contents change, so the UI can redraw.</summary>
        public event Action OnInventoryChanged;

        public InventorySystem(int slotCount)
        {
            if (slotCount < 1)
            {
                throw new ArgumentException("Slot count must be at least 1.", nameof(slotCount));
            }

            slots = new ItemStack[slotCount];
        }

        public int SlotCount => slots.Length;

        /// <summary>Returns the stack in a slot, or null if the slot is empty.</summary>
        public ItemStack GetSlot(int index)
        {
            return slots[index];
        }

        /// <summary>
        /// Raises OnInventoryChanged manually. Use this when a stack already in
        /// the inventory changed in place — for example its durability dropped —
        /// so the UI redraws even though no slot was added or removed.
        /// </summary>
        public void NotifyChanged()
        {
            OnInventoryChanged?.Invoke();
        }

        /// <summary>
        /// Adds items to the inventory. First fills existing matching stacks,
        /// then uses empty slots for the remainder. Returns the amount that did
        /// not fit (0 when everything was stored).
        /// </summary>
        public int AddItem(ItemData itemData, int amount)
        {
            if (itemData == null)
            {
                throw new ArgumentNullException(nameof(itemData));
            }

            if (amount < 1)
            {
                throw new ArgumentException("Amount must be at least 1.", nameof(amount));
            }

            int remaining = amount;

            remaining = FillExistingStacks(itemData, remaining);
            remaining = FillEmptySlots(itemData, remaining);

            if (remaining < amount)
            {
                OnInventoryChanged?.Invoke();
            }

            return remaining;
        }

        /// <summary>
        /// Removes up to <paramref name="amount"/> of an item type from anywhere
        /// in the inventory. Returns how many were actually removed.
        /// </summary>
        public int RemoveItem(ItemData itemData, int amount)
        {
            if (itemData == null)
            {
                throw new ArgumentNullException(nameof(itemData));
            }

            if (amount < 1)
            {
                throw new ArgumentException("Amount must be at least 1.", nameof(amount));
            }

            int remaining = amount;

            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                ItemStack stack = slots[i];
                if (stack == null || stack.ItemData != itemData)
                {
                    continue;
                }

                remaining -= stack.RemoveQuantity(remaining);

                if (stack.IsEmpty)
                {
                    slots[i] = null;
                }
            }

            int removed = amount - remaining;
            if (removed > 0)
            {
                OnInventoryChanged?.Invoke();
            }

            return removed;
        }

        /// <summary>Total count of an item type across all slots.</summary>
        public int CountItem(ItemData itemData)
        {
            int total = 0;
            foreach (ItemStack stack in slots)
            {
                if (stack != null && stack.ItemData == itemData)
                {
                    total += stack.Quantity;
                }
            }

            return total;
        }

        public bool HasFreeSlot()
        {
            return IndexOfFirstEmptySlot() >= 0;
        }

        private int FillExistingStacks(ItemData itemData, int remaining)
        {
            if (!itemData.IsStackable)
            {
                return remaining;
            }

            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                ItemStack stack = slots[i];
                if (stack == null || stack.ItemData != itemData || stack.IsFull)
                {
                    continue;
                }

                remaining = stack.AddQuantity(remaining);
            }

            return remaining;
        }

        private int FillEmptySlots(ItemData itemData, int remaining)
        {
            while (remaining > 0)
            {
                int emptyIndex = IndexOfFirstEmptySlot();
                if (emptyIndex < 0)
                {
                    break;
                }

                ItemStack newStack = new ItemStack(itemData, remaining);
                remaining -= newStack.Quantity;
                slots[emptyIndex] = newStack;
            }

            return remaining;
        }

        private int IndexOfFirstEmptySlot()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}