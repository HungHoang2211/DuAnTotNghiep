using System;
using UnityEngine;

namespace SimpleSurvival.Items
{
    [Serializable]
    public sealed class ItemStack
    {
        [SerializeField] private ItemData itemData;
        [SerializeField] private int quantity;
        [SerializeField] private int currentDurability;

        public ItemStack(ItemData itemData, int quantity)
        {
            Validate(itemData, quantity);
            this.itemData = itemData;
            this.quantity = Mathf.Min(quantity, itemData.MaxStack);
            this.currentDurability = itemData.MaxDurability;
        }

        public ItemStack(ItemData itemData, int quantity, int currentDurability)
        {
            Validate(itemData, quantity);
            this.itemData = itemData;
            this.quantity = Mathf.Min(quantity, itemData.MaxStack);
            this.currentDurability = Mathf.Clamp(currentDurability, 0, itemData.MaxDurability);
        }

        public ItemData ItemData => itemData;
        public int Quantity => quantity;
        public int CurrentDurability => currentDurability;

        public bool IsFull => quantity >= itemData.MaxStack;
        public bool IsEmpty => quantity <= 0;
        public bool IsBroken => itemData.IsDurable && currentDurability <= 0;

        public bool CanStackWith(ItemStack other)
        {
            return other != null
                && itemData.IsStackable
                && other.itemData == itemData;
        }

        public int AddQuantity(int amount)
        {
            if (amount < 1)
                throw new ArgumentException("Amount must be at least 1.", nameof(amount));

            int freeSpace = itemData.MaxStack - quantity;
            int accepted = Mathf.Min(amount, freeSpace);
            quantity += accepted;

            return amount - accepted;
        }

        public int RemoveQuantity(int amount)
        {
            if (amount < 1)
                throw new ArgumentException("Amount must be at least 1.", nameof(amount));

            int removed = Mathf.Min(amount, quantity);
            quantity -= removed;

            return removed;
        }

        public bool ReduceDurability()
        {
            if (!itemData.IsDurable || currentDurability <= 0)
                return false;

            currentDurability--;
            return currentDurability <= 0;
        }

        public float DurabilityRatio
        {
            get
            {
                if (!itemData.IsDurable)
                    return 1f;

                return (float)currentDurability / itemData.MaxDurability;
            }
        }

        public ItemStack Clone()
        {
            ItemStack copy = new ItemStack(itemData, quantity, currentDurability);
            return copy;
        }

        private static void Validate(ItemData itemData, int quantity)
        {
            if (itemData == null)
                throw new ArgumentNullException(nameof(itemData));

            if (quantity < 1)
                throw new ArgumentException("Quantity must be at least 1.", nameof(quantity));
        }
    }
}