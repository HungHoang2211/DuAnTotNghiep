using System.Collections.Generic;
using UnityEngine;
using SimpleSurvival.Items;

namespace SimpleSurvival.Loot
{
    [CreateAssetMenu(menuName = "Simple Survival/Loot/Loot Table", fileName = "LootTable")]
    public sealed class LootTable : ScriptableObject
    {
        [SerializeField] private string displayName;
        [SerializeField] private int slotCount = 12;
        [SerializeField] private List<LootEntry> entries = new List<LootEntry>();

        public string DisplayName => displayName;
        public int SlotCount => slotCount;
        public IReadOnlyList<LootEntry> Entries => entries;

        public List<ItemStack> Roll()
        {
            var result = new List<ItemStack>();
            foreach (var entry in entries)
            {
                ItemStack stack = entry.Roll();
                if (stack != null) result.Add(stack);
            }
            return result;
        }
    }
}