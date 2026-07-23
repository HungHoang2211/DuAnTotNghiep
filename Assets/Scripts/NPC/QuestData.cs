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

        [Header("Story Completion")]
        [Tooltip("Tick nếu quest này là quest cuối cùng, hoàn thành xong là coi như xong cốt truyện.")]
        [SerializeField] private bool marksStoryComplete = false;
        [Tooltip("Tên scene các map sẽ bị khoá vĩnh viễn ngay khi quest này hoàn thành.")]
        [SerializeField] private List<string> mapsToLockOnComplete = new List<string>();

        public string QuestId => questId;
        public string QuestName => questName;
        public string OfferDialogue => offerDialogue;
        public string TurnInDialogue => turnInDialogue;
        public IReadOnlyList<QuestObjectiveData> Objectives => objectives;
        public IReadOnlyList<QuestRewardEntry> Rewards => rewards;
        public bool MarksStoryComplete => marksStoryComplete;
        public IReadOnlyList<string> MapsToLockOnComplete => mapsToLockOnComplete;
    }
}