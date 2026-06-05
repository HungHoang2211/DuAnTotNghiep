using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.Items
{
    [CreateAssetMenu(menuName = "Simple Survival/Resource Node",
                     fileName = "ResourceNode_")]
    public class ResourceNodeSO : ScriptableObject
    {
        [System.Serializable]
        public struct LootEntry
        {
            public ItemData Item;
            public int AmountMin;
            public int AmountMax;
            [Tooltip("0 = luon drop. >0 = can skill level")]
            public int RequiredSkillLevel;
            [Range(0f, 1f)] public float BonusChance;
        }

        [Header("Loot")]
        public LootEntry[] Entries;

        [Header("Respawn")]
        public float RespawnTime = 10f;

        // Goi voi skillLevel=0 khi chua co skill tree
        public List<(ItemData item, int amount)> Roll(int skillLevel = 0)
        {
            var result = new List<(ItemData, int)>();
            foreach (var entry in Entries)
            {
                bool isBase = entry.RequiredSkillLevel == 0;
                bool isBonus = skillLevel >= entry.RequiredSkillLevel
                             && Random.value < entry.BonusChance;
                if (isBase || isBonus)
                    result.Add((entry.Item,
                        Random.Range(entry.AmountMin, entry.AmountMax + 1)));
            }
            return result;
        }
    }
}
