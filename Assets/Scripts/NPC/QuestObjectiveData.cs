using System;
using UnityEngine;
using SimpleSurvival.Items;
using SimpleSurvival.Stats;

namespace SimpleSurvival.Quests
{
    [Serializable]
    public class QuestObjectiveData
    {
        public QuestObjectiveType type;
        public string description;

        [Header("Dùng cho CollectItem")]
        public ItemData targetItem;

        [Header("Dùng cho KillEnemy")]
        public EnemyStatsConfig targetEnemyConfig;

        [Header("Dùng cho FindNPC")]
        public string targetNpcId;

        [Header("Dùng cho EscortNPC")]
        public string escortPointId;

        public int requiredAmount = 1;
    }
}