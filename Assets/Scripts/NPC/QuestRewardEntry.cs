using System;
using SimpleSurvival.Items;

namespace SimpleSurvival.Quests
{
    [Serializable]
    public class QuestRewardEntry
    {
        public ItemData itemData;
        public int quantity = 1;
    }
}