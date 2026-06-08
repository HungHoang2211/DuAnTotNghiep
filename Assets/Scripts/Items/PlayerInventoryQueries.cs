using UnityEngine;

namespace SimpleSurvival.Items
{
    [RequireComponent(typeof(PlayerInventory))]
    public class PlayerInventoryQueries : MonoBehaviour
    {
        private PlayerInventory _playerInventory;

        private void Awake()
        {
            _playerInventory = GetComponent<PlayerInventory>();
        }

        public bool HasTool(ToolType toolType)
        {
            return FindToolItem(toolType) != null;
        }

        public ItemStack FindToolItem(ToolType toolType)
        {
            ItemStack result = SearchInventory(_playerInventory.Pockets, toolType);
            if (result != null) return result;

            if (_playerInventory.Backpack != null)
                result = SearchInventory(_playerInventory.Backpack, toolType);

            return result;
        }

        public float GetToolDamage(ToolType toolType)
        {
            ItemStack stack = FindToolItem(toolType);
            if (stack == null) return 0f;

            ToolAbility tool = stack.ItemData.GetAbility<ToolAbility>();
            return tool != null ? tool.Damage : 0f;
        }

        public bool CanAddItem(ItemData itemData, int quantity)
        {
            if (itemData == null || quantity <= 0) return false;

            int remaining = quantity;

            remaining = SimulateFillStacks(_playerInventory.Pockets, itemData, remaining);
            if (remaining <= 0) return true;

            if (_playerInventory.Backpack != null)
                remaining = SimulateFillStacks(_playerInventory.Backpack, itemData, remaining);

            if (remaining <= 0) return true;

            if (_playerInventory.Pockets.HasFreeSlot()) return true;
            if (_playerInventory.Backpack != null && _playerInventory.Backpack.HasFreeSlot()) return true;

            return false;
        }

        public int AddItem(ItemData itemData, int quantity)
        {
            int remaining = _playerInventory.Pockets.AddItem(itemData, quantity);

            if (remaining > 0 && _playerInventory.Backpack != null)
                remaining = _playerInventory.Backpack.AddItem(itemData, remaining);

            return remaining;
        }

        private static ItemStack SearchInventory(InventorySystem inventory, ToolType toolType)
        {
            if (inventory == null) return null;

            for (int i = 0; i < inventory.SlotCount; i++)
            {
                ItemStack stack = inventory.GetSlot(i);
                if (stack == null) continue;
                if (!stack.ItemData.HasTag(ItemTag.Tool)) continue;

                ToolAbility tool = stack.ItemData.GetAbility<ToolAbility>();
                if (tool != null && tool.ToolType == toolType)
                    return stack;
            }

            return null;
        }

        private static int SimulateFillStacks(InventorySystem inventory, ItemData itemData, int amount)
        {
            if (inventory == null || !itemData.IsStackable) return amount;

            int remaining = amount;

            for (int i = 0; i < inventory.SlotCount && remaining > 0; i++)
            {
                ItemStack stack = inventory.GetSlot(i);
                if (stack == null || stack.ItemData != itemData) continue;

                int space = itemData.MaxStack - stack.Quantity;
                if (space <= 0) continue;

                int fit = Mathf.Min(space, remaining);
                remaining -= fit;
            }

            return remaining;
        }
    }
}