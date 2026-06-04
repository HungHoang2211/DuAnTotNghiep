using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.Items
{
    [CreateAssetMenu(menuName = "Simple Survival/Resource Node", fileName = "ResourceNode_")]
    public class ResourceNodeSO : ScriptableObject
    {
        [System.Serializable]
        public struct LootEntry
        {
            public ItemData Item;
            public int AmountMin;
            public int AmountMax;

            [Tooltip("0 = luôn drop. > 0 = yêu cầu skill level tối thiểu")]
            public int RequiredSkillLevel;

            [Range(0f, 1f)]
            [Tooltip("% nhận thêm khi đủ skill level (0 = luôn nhận nếu đủ level)")]
            public float BonusChance;
        }

        [Header("Identity")]
        public string NodeName;

        [Header("Loot")]
        public LootEntry[] Entries;

        [Header("Respawn")]
        public float RespawnTime = 10f;

        [Header("Skill Requirement")]
        [Tooltip("Skill level tối thiểu để harvest được node này")]
        public int RequiredSkillLevel = 0;

        /// Roll loot theo skill level. Truyền 0 khi chưa có skill tree.
        /// Trả về List(ItemData, amount) — inventory tự tạo ItemStack từ đây.
        public List<(ItemData item, int amount)> Roll(int skillLevel = 0)
        {
            var result = new List<(ItemData, int)>();

            foreach (var entry in Entries)
            {
                bool isBaseEntry = entry.RequiredSkillLevel == 0;
                bool isBonusEntry = skillLevel >= entry.RequiredSkillLevel
                                    && Random.value < entry.BonusChance;

                if (isBaseEntry || isBonusEntry)
                {
                    int amount = Random.Range(entry.AmountMin, entry.AmountMax + 1);
                    result.Add((entry.Item, amount));
                }
            }

            return result;
        }
    }
}