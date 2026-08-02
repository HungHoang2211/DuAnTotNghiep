using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleSurvival.Quests
{
    public sealed class QuestLogEntryUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text questNameText;
        [SerializeField] private TMP_Text objectiveText;

        // Button để bắt click vào slot. Nếu không gán trong Inspector, tự lấy Button trên cùng GameObject.
        [SerializeField] private Button entryButton;

        // Quest đang được gán cho slot này (do QuestLogUI set qua SetAssignedQuest).
        private QuestData _assignedQuest;

        // Bắn ra kèm quest đang được gán khi người chơi click vào slot này.
        public event Action<QuestData> OnEntryClicked;

        private void Awake()
        {
            if (entryButton == null) entryButton = GetComponent<Button>();
            if (entryButton != null) entryButton.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (entryButton != null) entryButton.onClick.RemoveListener(HandleClick);
        }

        private void HandleClick()
        {
            OnEntryClicked?.Invoke(_assignedQuest);
        }

        public void SetAssignedQuest(QuestData quest)
        {
            _assignedQuest = quest;
        }

        public void SetQuestName(string value)
        {
            if (questNameText != null) questNameText.text = value;
        }

        public void SetObjectiveText(string value)
        {
            if (objectiveText != null) objectiveText.text = value;
        }
    }
}