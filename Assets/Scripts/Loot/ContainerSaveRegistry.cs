using System.Collections.Generic;
using UnityEngine;
using SimpleSurvival.Items;
using SimpleSurvival.Loot;

namespace SimpleSurvival.SaveLoad
{
    public sealed class ContainerSaveRegistry : MonoBehaviour
    {
        public static ContainerSaveRegistry Instance { get; private set; }

        [SerializeField] private ItemDatabase itemDatabase;

        private InventorySerializer inventorySerializer;
        private readonly List<LootContainer> active = new List<LootContainer>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            inventorySerializer = new InventorySerializer(itemDatabase);
        }

        public void InitializePersistentContainer(LootContainer container)
        {
            if (string.IsNullOrWhiteSpace(container.ContainerId))
            {
                Debug.LogWarning($"[ContainerSaveRegistry] '{container.name}' bật Persist Across Sessions nhưng chưa điền Container Id — roll mặc định.");
                active.Add(container);
                container.InitializeDefault();
                return;
            }

            if (active.Exists(c => c != null && c.ContainerId == container.ContainerId))
                Debug.LogWarning($"[ContainerSaveRegistry] Container Id '{container.ContainerId}' bị trùng — '{container.name}' đang dùng chung Id với 1 container khác đã active.");

            active.Add(container);

            ContainerData saved = SaveService.Instance != null
                ? SaveService.Instance.GetContainerData(container.ContainerId)
                : null;

            if (saved != null)
            {
                container.InitializeEmpty();
                inventorySerializer.Restore(saved.inventory, container.Inventory);
            }
            else
            {
                container.InitializeDefault();
            }
        }

        public List<ContainerData> Capture()
        {
            active.RemoveAll(c => c == null);

            Dictionary<string, ContainerData> merged = new Dictionary<string, ContainerData>();

            if (SaveService.Instance != null)
            {
                foreach (ContainerData data in SaveService.Instance.GetAllContainerData())
                {
                    if (!string.IsNullOrWhiteSpace(data.containerId))
                        merged[data.containerId] = data;
                }
            }

            foreach (LootContainer c in active)
            {
                if (string.IsNullOrWhiteSpace(c.ContainerId))
                    continue;

                merged[c.ContainerId] = new ContainerData
                {
                    containerId = c.ContainerId,
                    inventory = inventorySerializer.Capture(c.Inventory)
                };
            }

            return new List<ContainerData>(merged.Values);
        }
    }
}