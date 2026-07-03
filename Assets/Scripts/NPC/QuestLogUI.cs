using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.Quests
{
    public sealed class QuestLogUI : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject panelRoot;

        [Header("Entry")]
        [SerializeField] private QuestLogEntryUI entryPrefab;
        [SerializeField] private Transform entryContainer;

        private readonly Dictionary<QuestData, QuestLogEntryUI> _entries = new Dictionary<QuestData, QuestLogEntryUI>();

        private void Start()
        {
            QuestManager manager = QuestManager.Instance;
            if (manager != null)
            {
                manager.OnQuestStarted += HandleQuestStarted;
                manager.OnObjectiveProgress += HandleProgress;
                manager.OnQuestCompleted += HandleQuestCompleted;
            }
        }

        private void OnDestroy()
        {
            QuestManager manager = QuestManager.Instance;
            if (manager != null)
            {
                manager.OnQuestStarted -= HandleQuestStarted;
                manager.OnObjectiveProgress -= HandleProgress;
                manager.OnQuestCompleted -= HandleQuestCompleted;
            }
        }

        private void HandleQuestStarted(QuestData quest)
        {
            if (entryPrefab == null || entryContainer == null) return;
            if (_entries.ContainsKey(quest)) return;

            QuestLogEntryUI entry = Instantiate(entryPrefab, entryContainer);
            entry.SetQuestName(quest.QuestName);
            entry.SetObjectiveText(BuildObjectiveText(quest, 0));
            _entries[quest] = entry;

            if (panelRoot != null) panelRoot.SetActive(true);
        }

        private void HandleProgress(QuestData quest, int objectiveIndex)
        {
            if (!_entries.TryGetValue(quest, out QuestLogEntryUI entry)) return;
            entry.SetObjectiveText(BuildObjectiveText(quest, objectiveIndex));
        }

        private void HandleQuestCompleted(QuestData quest)
        {
            if (!_entries.TryGetValue(quest, out QuestLogEntryUI entry)) return;
            Destroy(entry.gameObject);
            _entries.Remove(quest);
        }

        private string BuildObjectiveText(QuestData quest, int objectiveIndex)
        {
            QuestManager manager = QuestManager.Instance;
            if (manager == null) return string.Empty;

            var objective = quest.Objectives[objectiveIndex];
            int current = manager.GetObjectiveProgress(quest, objectiveIndex);
            return $"{objective.description} ({current}/{objective.requiredAmount})";
        }
    }
}