using System;
using System.Collections.Generic;
using SimpleSurvival.Items;

namespace SimpleSurvival.SaveLoad
{
    [Serializable]
    public sealed class EquipmentSlotData
    {
        public EquipSlot slot;
        public int index;
        public string itemId;
        public int quantity;
        public int durability;
    }

    [Serializable]
    public sealed class EquipmentData
    {
        public List<EquipmentSlotData> slots = new List<EquipmentSlotData>();
    }
}