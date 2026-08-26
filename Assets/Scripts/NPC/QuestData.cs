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
        [SerializeField] private string questGiverName;

        [Header("Dialogue")]
        [TextArea][SerializeField] private string offerDialogue;
        [TextArea][SerializeField] private string turnInDialogue;

        [Header("Objectives")]
        [SerializeField] private List<QuestObjectiveData> objectives = new List<QuestObjectiveData>();

        [Header("Rewards")]
        [SerializeField] private List<QuestRewardEntry> rewards = new List<QuestRewardEntry>();

        [Header("Progression")]
        [SerializeField] private int expReward;
        [SerializeField] private int requiredLevel = 1;

        [Header("Turn-In")]
        [SerializeField] private bool requiresNpcTurnIn = false;

        [Header("Story Completion")]
        [SerializeField] private bool marksStoryComplete = false;
        [SerializeField] private List<string> mapsToLockOnComplete = new List<string>();

        public int ExpReward => expReward;
        public int RequiredLevel => requiredLevel;
        public string QuestId => questId;
        public string QuestName => questName;
        public string QuestGiverName => questGiverName;
        public string OfferDialogue => offerDialogue;
        public string TurnInDialogue => turnInDialogue;
        public IReadOnlyList<QuestObjectiveData> Objectives => objectives;
        public IReadOnlyList<QuestRewardEntry> Rewards => rewards;
        public bool RequiresNpcTurnIn => requiresNpcTurnIn;
        public bool MarksStoryComplete => marksStoryComplete;
        public IReadOnlyList<string> MapsToLockOnComplete => mapsToLockOnComplete;
    }
}