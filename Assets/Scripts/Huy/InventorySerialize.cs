using SimpleSurvival.Items;
using UnityEngine;

namespace SimpleSurvival.SaveLoad
{
    public sealed class InventorySerializer
    {
        private readonly SavedStackResolver resolver;

        public InventorySerializer(ItemDatabase database)
        {
            resolver = new SavedStackResolver(database);
        }

        public PlayerInventoryData Capture(PlayerInventory inventory)
        {
            return new PlayerInventoryData
            {
                pockets = Capture(inventory.Pockets),
                backpack = Capture(inventory.Backpack)
            };
        }

        public InventoryData Capture(InventorySystem inventory)
        {
            InventoryData data = new InventoryData();
            if (inventory == null)
                return data;

            data.slotCount = inventory.SlotCount;
            for (int slot = 0; slot < inventory.SlotCount; slot++)
                AppendStack(data, inventory.GetSlot(slot), slot);

            return data;
        }

        public void Restore(InventoryData data, InventorySystem inventory)
        {
            if (inventory == null)
                return;

            ItemStack[] ordered = BuildSlots(data, inventory.SlotCount);
            inventory.ReplaceAll(ordered);
        }

        private void AppendStack(InventoryData data, ItemStack stack, int slot)
        {
            if (stack == null)
                return;

            string itemId = stack.ItemData.ItemId;
            if (string.IsNullOrWhiteSpace(itemId))
            {
                Debug.LogWarning($"Skipping stack at slot {slot}: '{stack.ItemData.ItemName}' has no itemId.");
                return;
            }

            data.stacks.Add(new ItemStackData
            {
                slot = slot,
                itemId = itemId,
                quantity = stack.Quantity,
                durability = stack.CurrentDurability
            });
        }

        private ItemStack[] BuildSlots(InventoryData data, int slotCount)
        {
            ItemStack[] ordered = new ItemStack[slotCount];
            if (data == null)
                return ordered;

            foreach (ItemStackData stackData in data.stacks)
                PlaceStack(ordered, stackData);

            return ordered;
        }

        private void PlaceStack(ItemStack[] ordered, ItemStackData stackData)
        {
            if (stackData.slot < 0 || stackData.slot >= ordered.Length)
            {
                Debug.LogWarning($"Dropping saved stack '{stackData.itemId}': slot {stackData.slot} is out of range.");
                return;
            }

            if (!resolver.TryResolve(stackData.itemId, stackData.quantity, stackData.durability, out ItemStack stack))
                return;

            ordered[stackData.slot] = stack;
        }
    }
}