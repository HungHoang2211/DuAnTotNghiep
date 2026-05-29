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
                throw new ArgumentException("Slot count must be at least 1.", nameof(slotCount));

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
        /// Replaces the contents of a slot with the given stack (or null). For
        /// use by drag-drop and similar operations that move stacks between
        /// known slots. Raises OnInventoryChanged.
        /// </summary>
        public void SetSlot(int index, ItemStack stack)
        {
            slots[index] = stack;
            OnInventoryChanged?.Invoke();
        }

        /// <summary>
        /// Moves a stack from one slot to another, possibly across inventories.
        /// If the destination is empty, the stack moves over. If it holds the
        /// same stackable item, the stacks merge (overflow stays at the source).
        /// Otherwise, the two stacks swap positions.
        /// </summary>
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
                    // Everything fit — clear the source slot.
                    fromInventory.slots[fromIndex] = null;
                }
                else if (overflow < source.Quantity)
                {
                    // Some items moved — remove only the amount that fit.
                    source.RemoveQuantity(source.Quantity - overflow);
                }
                else
                {
                    // Destination was full — nothing merged, swap instead.
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

        /// <summary>
        /// Adds items to the inventory, creating new stacks with full durability.
        /// First fills existing matching stacks, then uses empty slots.
        /// Returns the amount that did not fit (0 when everything was stored).
        /// </summary>
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

        /// <summary>
        /// Places a pre-built stack into the inventory as-is, preserving its
        /// durability. Use this for loot items that spawn with partial wear.
        /// For stackable items, merges into existing stacks first.
        /// Returns 0 if the stack fit, or the leftover quantity if it did not.
        /// </summary>
        public int AddStack(ItemStack stack)
        {
            if (stack == null)
                throw new ArgumentNullException(nameof(stack));

            int remaining = stack.Quantity;

            // Stackable items can merge into existing stacks.
            if (stack.ItemData.IsStackable)
                remaining = FillExistingStacks(stack.ItemData, remaining);

            // Place whatever is left into an empty slot, keeping the original
            // stack instance so its durability value is preserved.
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

        /// <summary>
        /// Removes up to <paramref name="amount"/> of an item type from anywhere
        /// in the inventory. Returns how many were actually removed.
        /// </summary>
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

        /// <summary>Total count of an item type across all slots.</summary>
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

        /// <summary>
        /// Merges stackable items of the same type up to their max stack size,
        /// then sorts everything by item name. Fires one OnInventoryChanged.
        /// Non-stackable stacks (weapons, armor) keep their original instances
        /// so durability values are preserved.
        /// </summary>
        public void Sort()
        {
            List<ItemStack> sorted = BuildSortedList(this);
            WriteBack(sorted, this);
            OnInventoryChanged?.Invoke();
        }

        /// <summary>
        /// Sorts two inventories as one combined bag — items from both are
        /// merged, sorted by name, then filled back into <paramref name="first"/>
        /// first and <paramref name="second"/> with the remainder.
        /// Fires OnInventoryChanged on both.
        /// </summary>
        public static void SortTogether(InventorySystem first, InventorySystem second)
        {
            // ── Collect from both ────────────────────────────────────────────
            List<InventorySystem> sources = new List<InventorySystem> { first, second };
            List<ItemStack> sorted = BuildSortedList(sources.ToArray());

            // ── Fill first, then second ───────────────────────────────────────
            int written = WriteBack(sorted, first, startAt: 0);
            List<ItemStack> remainder = sorted.GetRange(written, sorted.Count - written);
            WriteBack(remainder, second, startAt: 0);

            first.OnInventoryChanged?.Invoke();
            second.OnInventoryChanged?.Invoke();
        }

        // ── Sort helpers ─────────────────────────────────────────────────────

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

        /// <summary>
        /// Writes stacks into the inventory starting at slot 0.
        /// Returns the number of stacks actually written.
        /// </summary>
        private static int WriteBack(List<ItemStack> stacks, InventorySystem inventory,
            int startAt = 0)
        {
            int written = 0;
            for (int i = 0; i < inventory.slots.Length && written < stacks.Count; i++)
                inventory.slots[i] = stacks[written++];

            // Clear leftover slots.
            for (int i = written; i < inventory.slots.Length; i++)
                inventory.slots[i] = null;

            return written;
        }

        // ── Private helpers ──────────────────────────────────────────────────

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