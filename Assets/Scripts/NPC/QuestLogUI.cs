using SimpleSurvival.UI.Hud;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.Quests
{
    public sealed class QuestLogUI : MonoBehaviour
    {
        [Header("Fixed Slots")]
        [SerializeField] private List<QuestLogEntryUI> slots = new List<QuestLogEntryUI>();

        [Header("Quest Flow")]
        [SerializeField] private TutorialQuestSequencer sequencer;

        private readonly Dictionary<QuestData, QuestLogEntryUI> _assignedSlots = new Dictionary<QuestData, QuestLogEntryUI>();
        private readonly Queue<QuestData> _pendingQuests = new Queue<QuestData>();
        private readonly HashSet<QuestData> _readyForTurnIn = new HashSet<QuestData>();

        private void Start()
        {
            foreach (var slot in slots)
            {
                if (slot == null) continue;
                slot.gameObject.SetActive(false);
                slot.OnEntryClicked += HandleEntryClicked;
            }

            QuestManager manager = QuestManager.Instance;
            if (manager != null)
            {
                manager.OnQuestStarted += HandleQuestStarted;
                manager.OnObjectiveProgress += HandleProgress;
                manager.OnQuestReadyToTurnIn += HandleReadyToTurnIn;
                manager.OnQuestCompleted += HandleQuestCompleted;

                SyncExistingActiveQuests(manager);
            }
        }

        private void OnDestroy()
        {
            foreach (var slot in slots)
            {
                if (slot != null) slot.OnEntryClicked -= HandleEntryClicked;
            }

            QuestManager manager = QuestManager.Instance;
            if (manager != null)
            {
                manager.OnQuestStarted -= HandleQuestStarted;
                manager.OnObjectiveProgress -= HandleProgress;
                manager.OnQuestReadyToTurnIn -= HandleReadyToTurnIn;
                manager.OnQuestCompleted -= HandleQuestCompleted;
            }
        }

        private void SyncExistingActiveQuests(QuestManager manager)
        {
            foreach (QuestData quest in manager.GetActiveQuests())
            {
                if (_assignedSlots.ContainsKey(quest)) continue;

                HandleQuestStarted(quest);

                for (int i = 0; i < quest.Objectives.Count; i++)
                    HandleProgress(quest, i);

                if (manager.IsReadyToTurnIn(quest))
                    HandleReadyToTurnIn(quest);
            }
        }

        private void HandleEntryClicked(QuestData quest)
        {
            if (quest == null) return;

            if (_readyForTurnIn.Contains(quest))
            {
                if (quest.RequiresNpcTurnIn) return;

                bool completed = QuestManager.Instance != null && QuestManager.Instance.CompleteQuest(quest);
                if (!completed)
                    FollowNotifyManager.Instance?.Notify("Inventory is full! Make space to claim your reward.", SpeechHudType.Neutral);

                return;
            }

            sequencer?.RevealQuestHighlight(quest);
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

        private void HandleReadyToTurnIn(QuestData quest)
        {
            if (!_assignedSlots.TryGetValue(quest, out QuestLogEntryUI slot)) return;

            _readyForTurnIn.Add(quest);
            slot.SetObjectiveText(BuildTurnInText(quest));
            slot.SetReadyToTurnIn(true);
        }

        private void HandleQuestCompleted(QuestData quest)
        {
            if (!_assignedSlots.TryGetValue(quest, out QuestLogEntryUI slot)) return;

            _readyForTurnIn.Remove(quest);
            slot.SetReadyToTurnIn(false);
            slot.gameObject.SetActive(false);
            slot.SetAssignedQuest(null);
            _assignedSlots.Remove(quest);

            if (_pendingQuests.Count > 0)
            {
                QuestData nextQuest = _pendingQuests.Dequeue();
                AssignSlot(slot, nextQuest);
            }
        }

        private void AssignSlot(QuestLogEntryUI slot, QuestData quest)
        {
            slot.SetAssignedQuest(quest);
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

        private string BuildTurnInText(QuestData quest)
        {
            bool isEscort = false;
            foreach (var objective in quest.Objectives)
            {
                if (objective.type == QuestObjectiveType.EscortNPC)
                {
                    isEscort = true;
                    break;
                }
            }

            if (isEscort)
            {
                string giver = string.IsNullOrEmpty(quest.QuestGiverName) ? "the quest giver" : quest.QuestGiverName;
                return $"Talk to {giver} to receive your reward.";
            }

            if (quest.RequiresNpcTurnIn)
            {
                string giver = string.IsNullOrEmpty(quest.QuestGiverName) ? "the quest giver" : quest.QuestGiverName;
                return $"Go back to find {giver} to receive your reward.";
            }

            return "Tap to complete the quest.";
        }
    }
}