using System;
using UnityEngine;
using SimpleSurvival.Items;

namespace SimpleSurvival.Loot
{
    [Serializable]
    public sealed class LootEntry
    {
        [SerializeField] private ItemData itemData;
        [SerializeField, Range(0f, 1f)] private float dropChance = 1f;
        [SerializeField] private int minQuantity = 1;
        [SerializeField] private int maxQuantity = 1;
        [SerializeField] private bool guaranteed = false;

        [Header("Durability (durable items only)")]
        [SerializeField, Range(0f, 1f)] private float minDurabilityPercent = 1f;
        [SerializeField, Range(0f, 1f)] private float maxDurabilityPercent = 1f;

        public ItemData ItemData => itemData;
        public float DropChance => dropChance;
        public int MinQuantity => minQuantity;
        public int MaxQuantity => maxQuantity;
        public bool Guaranteed => guaranteed;
        public float MinDurabilityPercent => minDurabilityPercent;
        public float MaxDurabilityPercent => maxDurabilityPercent;

        public ItemStack Roll()
        {
            if (itemData == null) return null;
            if (!guaranteed && UnityEngine.Random.value > dropChance) return null;

            int qty = UnityEngine.Random.Range(minQuantity, maxQuantity + 1);
            if (qty < 1) return null;

            if (!itemData.IsDurable)
                return new ItemStack(itemData, qty);

            float percent = UnityEngine.Random.Range(minDurabilityPercent, maxDurabilityPercent);
            int durability = Mathf.Max(1, Mathf.RoundToInt(itemData.MaxDurability * percent));
            return new ItemStack(itemData, qty, durability);
        }
    }
}