using System;
using UnityEngine;

namespace SimpleSurvival.Items
{
    /// <summary>
    /// One stack of items as it exists in the world at runtime: which item type,
    /// how many, and how much durability is left. This is the mutable counterpart
    /// to ItemData. Inventory cells hold ItemStacks.
    /// </summary>
    [Serializable]
    public sealed class ItemStack
    {
        [SerializeField] private ItemData itemData;
        [SerializeField] private int quantity;
        [SerializeField] private int currentDurability;

        public ItemStack(ItemData itemData, int quantity)
        {
            if (itemData == null)
            {
                throw new ArgumentNullException(nameof(itemData));
            }

            if (quantity < 1)
            {
                throw new ArgumentException("Quantity must be at least 1.", nameof(quantity));
            }

            this.itemData = itemData;
            this.quantity = Mathf.Min(quantity, itemData.MaxStack);
            this.currentDurability = itemData.MaxDurability;
        }

        public ItemData ItemData => itemData;
        public int Quantity => quantity;
        public int CurrentDurability => currentDurability;

        public bool IsFull => quantity >= itemData.MaxStack;
        public bool IsBroken => itemData.IsDurable && currentDurability <= 0;

        /// <summary>
        /// True when another stack holds the same stackable item type and so
        /// could receive items from this one.
        /// </summary>
        public bool CanStackWith(ItemStack other)
        {
            return other != null
                && itemData.IsStackable
                && other.itemData == itemData;
        }

        /// <summary>
        /// Adds up to <paramref name="amount"/> items, respecting the max stack
        /// size. Returns the amount that did not fit (0 when all fit).
        /// </summary>
        public int AddQuantity(int amount)
        {
            if (amount < 1)
            {
                throw new ArgumentException("Amount must be at least 1.", nameof(amount));
            }

            int freeSpace = itemData.MaxStack - quantity;
            int accepted = Mathf.Min(amount, freeSpace);
            quantity += accepted;

            return amount - accepted;
        }

        /// <summary>
        /// Removes up to <paramref name="amount"/> items. Returns how many were
        /// actually removed (capped at the current quantity).
        /// </summary>
        public int RemoveQuantity(int amount)
        {
            if (amount < 1)
            {
                throw new ArgumentException("Amount must be at least 1.", nameof(amount));
            }

            int removed = Mathf.Min(amount, quantity);
            quantity -= removed;

            return removed;
        }

        public bool IsEmpty => quantity <= 0;

        /// <summary>
        /// Reduces durability by one use. Does nothing for items that never
        /// wear out. Returns true on the use that breaks the item.
        /// </summary>
        public bool ReduceDurability()
        {
            if (!itemData.IsDurable || currentDurability <= 0)
            {
                return false;
            }

            currentDurability--;
            return currentDurability <= 0;
        }

        public float DurabilityRatio
        {
            get
            {
                if (!itemData.IsDurable)
                {
                    return 1f;
                }

                return (float)currentDurability / itemData.MaxDurability;
            }
        }

        /// <summary>
        /// Creates an independent copy. Inventory code clones a stack before
        /// storing it so two cells can never share — and silently corrupt —
        /// the same instance.
        /// </summary>
        public ItemStack Clone()
        {
            ItemStack copy = new ItemStack(itemData, quantity);
            copy.currentDurability = currentDurability;
            return copy;
        }
    }
}
