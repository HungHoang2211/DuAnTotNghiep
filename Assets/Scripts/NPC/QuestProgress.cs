using UnityEngine;

namespace SimpleSurvival.Quests
{
    public sealed class QuestProgress
    {
        public QuestData Quest { get; }
        private readonly int[] _currentAmounts;

        public QuestProgress(QuestData quest)
        {
            Quest = quest;
            _currentAmounts = new int[quest.Objectives.Count];
        }

        public int GetAmount(int index) => _currentAmounts[index];

        public void AddProgress(int index, int amount)
        {
            int required = Quest.Objectives[index].requiredAmount;
            _currentAmounts[index] = Mathf.Min(_currentAmounts[index] + amount, required);
        }

        public bool IsObjectiveComplete(int index)
        {
            return _currentAmounts[index] >= Quest.Objectives[index].requiredAmount;
        }

        public bool IsAllComplete()
        {
            for (int i = 0; i < _currentAmounts.Length; i++)
            {
                if (!IsObjectiveComplete(i)) return false;
            }
            return true;
        }
    }
}