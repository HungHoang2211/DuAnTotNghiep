using System;
using System.Collections.Generic;

namespace SimpleSurvival.SaveLoad
{
    [Serializable]
    public sealed class ItemStackData
    {
        public int slot;
        public string itemId;
        public int quantity;
        public int durability;
    }

    [Serializable]
    public sealed class InventoryData
    {
        public int slotCount;
        public List<ItemStackData> stacks = new List<ItemStackData>();
    }

    [Serializable]
    public sealed class PlayerInventoryData
    {
        public InventoryData pockets = new InventoryData();
        public InventoryData backpack = new InventoryData();
    }
}