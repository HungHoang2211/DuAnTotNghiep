using System;
using SimpleSurvival.Items;
using SimpleSurvival.Stats;

namespace SimpleSurvival.Quests
{
    [Serializable]
    public class QuestObjectiveData
    {
        public QuestObjectiveType type;
        public string description;
        public ItemData targetItem;
        public EnemyStatsConfig targetEnemyConfig;
        public int requiredAmount = 1;
    }
}