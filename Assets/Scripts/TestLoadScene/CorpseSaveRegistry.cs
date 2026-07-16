using System.Collections.Generic;
using UnityEngine;
using SimpleSurvival.Items;
using SimpleSurvival.Loot;
using SimpleSurvival.Player;

namespace SimpleSurvival.SaveLoad
{
    public sealed class CorpseSaveRegistry : MonoBehaviour
    {
        public static CorpseSaveRegistry Instance { get; private set; }

        [SerializeField] private ItemDatabase itemDatabase;
        [SerializeField] private GameObject corpsePrefab;

        private SavedStackResolver resolver;
        private readonly List<Entry> active = new List<Entry>();
        private readonly HashSet<string> seededMaps = new HashSet<string>();

        private sealed class Entry
        {
            public GameObject instance;
            public string mapId;
            public LootContainer container;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            resolver = new SavedStackResolver(itemDatabase);
        }

        public GameObject SpawnCorpseObject(string mapId, Vector3 position, List<ItemStack> items)
        {
            GameObject obj = Instantiate(corpsePrefab, position, Quaternion.identity);

            LootContainer container = obj.GetComponent<LootContainer>();
            container?.InitializeRuntimeWithStacks(items);

            PlayerCorpseMapBinding binding = obj.AddComponent<PlayerCorpseMapBinding>();
            binding.Initialize(mapId);

            active.Add(new Entry { instance = obj, mapId = mapId, container = container });
            return obj;
        }

        public List<CorpseData> Capture()
        {
            active.RemoveAll(e => e.instance == null);

            List<CorpseData> result = new List<CorpseData>();
            foreach (Entry e in active)
            {
                if (e.container == null || e.container.IsEmpty)
                    continue;

                Vector3 pos = e.instance.transform.position;
                result.Add(new CorpseData
                {
                    mapId = e.mapId,
                    x = pos.x,
                    y = pos.y,
                    z = pos.z,
                    items = CaptureItems(e.container.Inventory)
                });
            }
            return result;
        }

        public void RestoreForMap(string mapId)
        {
            if (seededMaps.Contains(mapId))
                return;
            seededMaps.Add(mapId);

            if (SaveService.Instance == null)
                return;

            List<CorpseData> saved = SaveService.Instance.GetCorpsesForMap(mapId);
            foreach (CorpseData data in saved)
            {
                List<ItemStack> items = ResolveItems(data.items);
                if (items.Count == 0)
                    continue;

                Vector3 position = new Vector3(data.x, data.y, data.z);
                SpawnCorpseObject(mapId, position, items);
            }
        }

        private List<ItemStackData> CaptureItems(InventorySystem inventory)
        {
            List<ItemStackData> list = new List<ItemStackData>();
            if (inventory == null)
                return list;

            for (int i = 0; i < inventory.SlotCount; i++)
            {
                ItemStack stack = inventory.GetSlot(i);
                if (stack == null) continue;

                string itemId = stack.ItemData.ItemId;
                if (string.IsNullOrWhiteSpace(itemId)) continue;

                list.Add(new ItemStackData
                {
                    slot = i,
                    itemId = itemId,
                    quantity = stack.Quantity,
                    durability = stack.CurrentDurability
                });
            }
            return list;
        }

        private List<ItemStack> ResolveItems(List<ItemStackData> data)
        {
            List<ItemStack> list = new List<ItemStack>();
            if (data == null)
                return list;

            foreach (ItemStackData d in data)
            {
                if (resolver.TryResolve(d.itemId, d.quantity, d.durability, out ItemStack stack))
                    list.Add(stack);
            }
            return list;
        }
    }
}