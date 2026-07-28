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

        [Header("CollectItem / HarvestNode / CraftItem")]
        public ItemData targetItem;

        [Header("KillEnemy")]
        public EnemyStatsConfig targetEnemyConfig;

        [Header("FindNPC")]
        public string targetNpcId;

        [Header("EscortNPC")]
        public List<string> escortWaypointIds = new List<string>();

        public int requiredAmount = 1;
    }
}