using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.Items
{
    [CreateAssetMenu(menuName = "Simple Survival/Item Database", fileName = "ItemDatabase")]
    public sealed class ItemDatabase : ScriptableObject
    {
        [SerializeField] private List<ItemData> items = new List<ItemData>();

        private Dictionary<string, ItemData> _itemsById;

        public IReadOnlyList<ItemData> Items => items;

        public bool TryGet(string itemId, out ItemData item)
        {
            EnsureLookup();
            return _itemsById.TryGetValue(itemId, out item);
        }

        public void SetItems(IEnumerable<ItemData> source)
        {
            items = new List<ItemData>(source);
            BuildLookup();
        }

        private void OnEnable()
        {
            BuildLookup();
        }

        private void EnsureLookup()
        {
            if (_itemsById == null)
                BuildLookup();
        }

        private void BuildLookup()
        {
            _itemsById = new Dictionary<string, ItemData>();
            foreach (ItemData item in items)
                Register(item);
        }

        private void Register(ItemData item)
        {
            if (item == null)
                return;
            if (string.IsNullOrWhiteSpace(item.ItemId))
                return;
            if (_itemsById.ContainsKey(item.ItemId))
                return;

            _itemsById.Add(item.ItemId, item);
        }
    }
}