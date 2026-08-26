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
        [SerializeField] private Button entryButton;

        private QuestData _assignedQuest;
        private QuestSlotReadyEffect _readyEffect;

        public event Action<QuestData> OnEntryClicked;

        private void Awake()
        {
            if (entryButton == null) entryButton = GetComponent<Button>();
            if (entryButton != null) entryButton.onClick.AddListener(HandleClick);
            _readyEffect = GetComponent<QuestSlotReadyEffect>();
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

        public void SetReadyToTurnIn(bool ready)
        {
            _readyEffect?.SetActive(ready);
        }
    }
}