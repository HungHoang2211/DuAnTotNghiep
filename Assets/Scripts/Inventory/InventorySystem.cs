using System;
using System.Collections.Generic;

namespace SimpleSurvival.Items
{
    public sealed class InventorySystem
    {
        private readonly ItemStack[] slots;

        public event Action OnInventoryChanged;

        public InventorySystem(int slotCount)
        {
            if (slotCount < 1)
                throw new ArgumentException("Slot count must be at least 1.", nameof(slotCount));

            slots = new ItemStack[slotCount];
        }

        public int SlotCount => slots.Length;

        public ItemStack GetSlot(int index)
        {
            return slots[index];
        }

        public void NotifyChanged()
        {
            OnInventoryChanged?.Invoke();
        }

        public void SetSlot(int index, ItemStack stack)
        {
            slots[index] = stack;
            OnInventoryChanged?.Invoke();
        }

        public static void TransferOrSwap(
            InventorySystem fromInventory, int fromIndex,
            InventorySystem toInventory, int toIndex)
        {
            if (fromInventory == toInventory && fromIndex == toIndex)
                return;

            ItemStack source = fromInventory.slots[fromIndex];
            if (source == null)
                return;

            ItemStack destination = toInventory.slots[toIndex];

            if (destination == null)
            {
                toInventory.slots[toIndex] = source;
                fromInventory.slots[fromIndex] = null;
            }
            else if (destination.CanStackWith(source))
            {
                int overflow = destination.AddQuantity(source.Quantity);
                if (overflow == 0)
                {
                    fromInventory.slots[fromIndex] = null;
                }
                else if (overflow < source.Quantity)
                {
                    source.RemoveQuantity(source.Quantity - overflow);
                }
                else
                {
                    toInventory.slots[toIndex] = source;
                    fromInventory.slots[fromIndex] = destination;
                }
            }
            else
            {
                toInventory.slots[toIndex] = source;
                fromInventory.slots[fromIndex] = destination;
            }

            fromInventory.OnInventoryChanged?.Invoke();
            if (toInventory != fromInventory)
                toInventory.OnInventoryChanged?.Invoke();
        }

        public int AddItem(ItemData itemData, int amount)
        {
            if (itemData == null)
                throw new ArgumentNullException(nameof(itemData));

            if (amount < 1)
                throw new ArgumentException("Amount must be at least 1.", nameof(amount));

            int remaining = amount;

            remaining = FillExistingStacks(itemData, remaining);
            remaining = FillEmptySlots(itemData, remaining);

            if (remaining < amount)
                OnInventoryChanged?.Invoke();

            return remaining;
        }

        public int AddStack(ItemStack stack)
        {
            if (stack == null)
                throw new ArgumentNullException(nameof(stack));

            int remaining = stack.Quantity;

            if (stack.ItemData.IsStackable)
                remaining = FillExistingStacks(stack.ItemData, remaining);

            if (remaining > 0)
            {
                int emptyIndex = IndexOfFirstEmptySlot();
                if (emptyIndex >= 0)
                {
                    slots[emptyIndex] = stack;
                    remaining = 0;
                }
            }

            if (remaining < stack.Quantity)
                OnInventoryChanged?.Invoke();

            return remaining;
        }

        public int RemoveItem(ItemData itemData, int amount)
        {
            if (itemData == null)
                throw new ArgumentNullException(nameof(itemData));

            if (amount < 1)
                throw new ArgumentException("Amount must be at least 1.", nameof(amount));

            int remaining = amount;

            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                ItemStack stack = slots[i];
                if (stack == null || stack.ItemData != itemData)
                    continue;

                remaining -= stack.RemoveQuantity(remaining);

                if (stack.IsEmpty)
                    slots[i] = null;
            }

            int removed = amount - remaining;
            if (removed > 0)
                OnInventoryChanged?.Invoke();

            return removed;
        }

        public int CountItem(ItemData itemData)
        {
            int total = 0;
            foreach (ItemStack stack in slots)
            {
                if (stack != null && stack.ItemData == itemData)
                    total += stack.Quantity;
            }

            return total;
        }

        public bool HasFreeSlot()
        {
            return IndexOfFirstEmptySlot() >= 0;
        }

        public void Sort()
        {
            List<ItemStack> sorted = BuildSortedList(this);
            WriteBack(sorted, this);
            OnInventoryChanged?.Invoke();
        }

        public static void SortTogether(InventorySystem first, InventorySystem second)
        {
            List<InventorySystem> sources = new List<InventorySystem> { first, second };
            List<ItemStack> sorted = BuildSortedList(sources.ToArray());

            int written = WriteBack(sorted, first, startAt: 0);
            List<ItemStack> remainder = sorted.GetRange(written, sorted.Count - written);
            WriteBack(remainder, second, startAt: 0);

            first.OnInventoryChanged?.Invoke();
            second.OnInventoryChanged?.Invoke();
        }

        private static List<ItemStack> BuildSortedList(params InventorySystem[] inventories)
        {
            Dictionary<ItemData, int> stackableTotals = new Dictionary<ItemData, int>();
            List<ItemStack> nonStackables = new List<ItemStack>();

            foreach (InventorySystem inventory in inventories)
            {
                for (int i = 0; i < inventory.slots.Length; i++)
                {
                    ItemStack stack = inventory.slots[i];
                    if (stack == null)
                        continue;

                    if (stack.ItemData.IsStackable)
                    {
                        if (!stackableTotals.ContainsKey(stack.ItemData))
                            stackableTotals[stack.ItemData] = 0;
                        stackableTotals[stack.ItemData] += stack.Quantity;
                    }
                    else
                    {
                        nonStackables.Add(stack);
                    }
                }
            }

            List<ItemStack> merged = new List<ItemStack>();
            foreach (KeyValuePair<ItemData, int> entry in stackableTotals)
            {
                int remaining = entry.Value;
                while (remaining > 0)
                {
                    int amount = UnityEngine.Mathf.Min(remaining, entry.Key.MaxStack);
                    merged.Add(new ItemStack(entry.Key, amount));
                    remaining -= amount;
                }
            }

            merged.AddRange(nonStackables);
            merged.Sort((a, b) =>
                string.Compare(a.ItemData.ItemName, b.ItemData.ItemName,
                    System.StringComparison.Ordinal));

            return merged;
        }

        private static int WriteBack(List<ItemStack> stacks, InventorySystem inventory,
            int startAt = 0)
        {
            int written = 0;
            for (int i = 0; i < inventory.slots.Length && written < stacks.Count; i++)
                inventory.slots[i] = stacks[written++];

            for (int i = written; i < inventory.slots.Length; i++)
                inventory.slots[i] = null;

            return written;
        }


        private int FillExistingStacks(ItemData itemData, int remaining)
        {
            if (!itemData.IsStackable)
                return remaining;

            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                ItemStack stack = slots[i];
                if (stack == null || stack.ItemData != itemData || stack.IsFull)
                    continue;

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
                    break;

                ItemStack newStack = new ItemStack(itemData, remaining);
                remaining -= newStack.Quantity;
                slots[emptyIndex] = newStack;
            }

            return remaining;
        }

        public int IndexOfFirstEmptySlot()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                    return i;
            }

            return -1;
        }
    }
}