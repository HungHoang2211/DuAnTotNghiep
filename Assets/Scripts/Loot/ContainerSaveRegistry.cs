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
            active.Add(container);

            if (string.IsNullOrWhiteSpace(container.ContainerId))
            {
                Debug.LogWarning($"[ContainerSaveRegistry] '{container.name}' bật Persist Across Sessions nhưng chưa điền Container Id — roll mặc định.");
                container.InitializeDefault();
                return;
            }

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

            List<ContainerData> result = new List<ContainerData>();
            foreach (LootContainer c in active)
            {
                if (string.IsNullOrWhiteSpace(c.ContainerId))
                    continue;

                result.Add(new ContainerData
                {
                    containerId = c.ContainerId,
                    inventory = inventorySerializer.Capture(c.Inventory)
                });
            }
            return result;
        }
    }
}