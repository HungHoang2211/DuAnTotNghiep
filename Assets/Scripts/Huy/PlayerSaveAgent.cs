using SimpleSurvival.Items;
using SimpleSurvival.Stats;
using UnityEngine;

namespace SimpleSurvival.SaveLoad
{
    public sealed class PlayerSaveAgent : MonoBehaviour
    {
        [SerializeField] private ItemDatabase itemDatabase;
        [SerializeField] private PlayerStats stats;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private PlayerEquipment equipment;

        private StatsSerializer statsSerializer;
        private InventorySerializer inventorySerializer;
        private EquipmentSerializer equipmentSerializer;

        private void Awake()
        {
            statsSerializer = new StatsSerializer();
            inventorySerializer = new InventorySerializer(itemDatabase);
            equipmentSerializer = new EquipmentSerializer(itemDatabase);
        }

        public PlayerData Capture()
        {
            return new PlayerData
            {
                stats = statsSerializer.Capture(stats),
                equipment = equipmentSerializer.Capture(equipment.System),
                inventory = inventorySerializer.Capture(inventory),
                placement = CapturePlacement()
            };
        }

        public void Restore(PlayerData data)
        {
            if (data == null)
                return;

            statsSerializer.Restore(data.stats, stats);
            equipmentSerializer.Restore(data.equipment, equipment.System);
            ResizeBackpackToFitEquipment();
            inventorySerializer.Restore(data.inventory.pockets, inventory.Pockets);
            inventorySerializer.Restore(data.inventory.backpack, inventory.Backpack);
            RestorePlacement(data.placement);
        }

        private void ResizeBackpackToFitEquipment()
        {
            inventory.ResizeBackpack(ReadEquippedBackpackSlots());
        }

        private int ReadEquippedBackpackSlots()
        {
            ItemStack backpackStack = equipment.System.GetSlot(EquipSlot.Backpack, 0);
            if (backpackStack == null)
                return 0;

            ContainerAbility container = backpackStack.ItemData.GetAbility<ContainerAbility>();
            return container != null ? container.ExtraSlots : 0;
        }

        private PlayerPlacementData CapturePlacement()
        {
            Vector3 position = transform.position;
            return new PlayerPlacementData
            {
                x = position.x,
                y = position.y,
                z = position.z,
                yaw = transform.eulerAngles.y
            };
        }

        private void RestorePlacement(PlayerPlacementData placement)
        {
            if (placement == null)
                return;

            transform.position = new Vector3(placement.x, placement.y, placement.z);
            transform.rotation = Quaternion.Euler(0f, placement.yaw, 0f);
        }
    }
}