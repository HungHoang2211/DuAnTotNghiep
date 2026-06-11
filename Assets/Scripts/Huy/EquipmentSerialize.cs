using SimpleSurvival.Items;
using UnityEngine;

namespace SimpleSurvival.SaveLoad
{
    public sealed class EquipmentSerializer
    {
        private readonly SavedStackResolver resolver;

        public EquipmentSerializer(ItemDatabase database)
        {
            resolver = new SavedStackResolver(database);
        }

        public EquipmentData Capture(EquipmentSystem equipment)
        {
            EquipmentData data = new EquipmentData();
            if (equipment == null)
                return data;

            foreach (EquipSlot slot in equipment.Slots)
                CaptureSlot(data, equipment, slot);

            return data;
        }

        public void Restore(EquipmentData data, EquipmentSystem equipment)
        {
            if (equipment == null)
                return;

            ClearAll(equipment);
            if (data == null)
                return;

            foreach (EquipmentSlotData slotData in data.slots)
                RestoreSlot(slotData, equipment);
        }

        private void CaptureSlot(EquipmentData data, EquipmentSystem equipment, EquipSlot slot)
        {
            int count = equipment.SlotCount(slot);
            for (int index = 0; index < count; index++)
                AppendStack(data, slot, index, equipment.GetSlot(slot, index));
        }

        private void AppendStack(EquipmentData data, EquipSlot slot, int index, ItemStack stack)
        {
            if (stack == null)
                return;

            string itemId = stack.ItemData.ItemId;
            if (string.IsNullOrWhiteSpace(itemId))
            {
                Debug.LogWarning($"Skipping equipped item in {slot}[{index}]: '{stack.ItemData.ItemName}' has no itemId.");
                return;
            }

            data.slots.Add(new EquipmentSlotData
            {
                slot = slot,
                index = index,
                itemId = itemId,
                quantity = stack.Quantity,
                durability = stack.CurrentDurability
            });
        }

        private void ClearAll(EquipmentSystem equipment)
        {
            foreach (EquipSlot slot in equipment.Slots)
            {
                int count = equipment.SlotCount(slot);
                for (int index = 0; index < count; index++)
                    equipment.SetSlotDirect(slot, index, null);
            }
        }

        private void RestoreSlot(EquipmentSlotData slotData, EquipmentSystem equipment)
        {
            if (!IsValidSlot(slotData, equipment))
            {
                Debug.LogWarning($"Dropping equipped item '{slotData.itemId}': slot {slotData.slot}[{slotData.index}] does not exist.");
                return;
            }

            if (!resolver.TryResolve(slotData.itemId, slotData.quantity, slotData.durability, out ItemStack stack))
                return;

            equipment.SetSlotDirect(slotData.slot, slotData.index, stack);
        }

        private bool IsValidSlot(EquipmentSlotData slotData, EquipmentSystem equipment)
        {
            if (!equipment.HasSlot(slotData.slot))
                return false;

            return slotData.index >= 0 && slotData.index < equipment.SlotCount(slotData.slot);
        }
    }
}