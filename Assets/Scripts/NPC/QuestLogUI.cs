using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.Quests
{
    public sealed class QuestLogUI : MonoBehaviour
    {
        [Header("Fixed Slots")]
        [SerializeField] private List<QuestLogEntryUI> slots = new List<QuestLogEntryUI>();

        private readonly Dictionary<QuestData, QuestLogEntryUI> _assignedSlots = new Dictionary<QuestData, QuestLogEntryUI>();
        private readonly Queue<QuestData> _pendingQuests = new Queue<QuestData>();

        private void Start()
        {
            foreach (var slot in slots)
            {
                if (slot != null) slot.gameObject.SetActive(false);
            }

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
            if (_assignedSlots.ContainsKey(quest)) return;

            QuestLogEntryUI freeSlot = FindFreeSlot();
            if (freeSlot == null)
            {
                _pendingQuests.Enqueue(quest);
                return;
            }

            AssignSlot(freeSlot, quest);
        }

        private void HandleProgress(QuestData quest, int objectiveIndex)
        {
            if (!_assignedSlots.TryGetValue(quest, out QuestLogEntryUI slot)) return;
            slot.SetObjectiveText(BuildObjectiveText(quest, objectiveIndex));
        }

        private void HandleQuestCompleted(QuestData quest)
        {
            if (!_assignedSlots.TryGetValue(quest, out QuestLogEntryUI slot)) return;

            slot.gameObject.SetActive(false);
            _assignedSlots.Remove(quest);

            if (_pendingQuests.Count > 0)
            {
                QuestData nextQuest = _pendingQuests.Dequeue();
                AssignSlot(slot, nextQuest);
            }
        }

        private void AssignSlot(QuestLogEntryUI slot, QuestData quest)
        {
            slot.SetQuestName(quest.QuestName);
            slot.SetObjectiveText(BuildObjectiveText(quest, 0));
            slot.gameObject.SetActive(true);
            _assignedSlots[quest] = slot;
        }

        private QuestLogEntryUI FindFreeSlot()
        {
            foreach (var slot in slots)
            {
                if (slot == null) continue;
                if (!slot.gameObject.activeSelf) return slot;
            }
            return null;
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