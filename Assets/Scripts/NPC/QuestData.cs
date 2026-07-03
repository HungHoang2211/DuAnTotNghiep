using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.Quests
{
    [CreateAssetMenu(menuName = "Simple Survival/Quests/Quest Data", fileName = "NewQuest")]
    public sealed class QuestData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string questId;
        [SerializeField] private string questName;

        [Header("Dialogue")]
        [TextArea][SerializeField] private string offerDialogue;
        [TextArea][SerializeField] private string turnInDialogue;

        [Header("Objectives")]
        [SerializeField] private List<QuestObjectiveData> objectives = new List<QuestObjectiveData>();

        [Header("Rewards")]
        [SerializeField] private List<QuestRewardEntry> rewards = new List<QuestRewardEntry>();

        public string QuestId => questId;
        public string QuestName => questName;
        public string OfferDialogue => offerDialogue;
        public string TurnInDialogue => turnInDialogue;
        public IReadOnlyList<QuestObjectiveData> Objectives => objectives;
        public IReadOnlyList<QuestRewardEntry> Rewards => rewards;
    }
}