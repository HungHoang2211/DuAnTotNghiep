using System;
using System.Collections.Generic;
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

        [Header("Dùng cho FindNPC / EscortNPC - phải khớp npcId trên NPC")]
        public string targetNpcId;

        [Header("Dùng cho EscortNPC - danh sách pointId theo đúng thứ tự đi qua, điểm cuối là đích")]
        public List<string> escortWaypointIds = new List<string>();

        public int requiredAmount = 1;
    }
}