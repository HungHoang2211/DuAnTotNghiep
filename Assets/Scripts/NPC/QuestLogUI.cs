using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.Quests
{
    public sealed class QuestLogUI : MonoBehaviour
    {
        [Header("Fixed Slots")]
        [SerializeField] private List<QuestLogEntryUI> slots = new List<QuestLogEntryUI>();

        [Header("Quest Flow")]
        // Dùng để bật highlight visual của quest tutorial khi người chơi click vào slot tương ứng.
        [SerializeField] private TutorialQuestSequencer sequencer;

        private readonly Dictionary<QuestData, QuestLogEntryUI> _assignedSlots = new Dictionary<QuestData, QuestLogEntryUI>();
        private readonly Queue<QuestData> _pendingQuests = new Queue<QuestData>();

        private void Start()
        {
            foreach (var slot in slots)
            {
                if (slot == null) continue;
                slot.gameObject.SetActive(false);
                // Đăng ký 1 lần cho toàn bộ vòng đời slot (slot cố định, chỉ đổi quest được gán bên trong).
                slot.OnEntryClicked += HandleEntryClicked;
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
            foreach (var slot in slots)
            {
                if (slot != null) slot.OnEntryClicked -= HandleEntryClicked;
            }

            QuestManager manager = QuestManager.Instance;
            if (manager != null)
            {
                manager.OnQuestStarted -= HandleQuestStarted;
                manager.OnObjectiveProgress -= HandleProgress;
                manager.OnQuestCompleted -= HandleQuestCompleted;
            }
        }

        // Click vào thông tin nhiệm vụ trong Quest Log -> bật highlight visual cho vật chỉ định của quest đó
        // (thay vì bật khi ấn dấu "!" như trước).
        private void HandleEntryClicked(QuestData quest)
        {
            if (quest != null) sequencer?.RevealQuestHighlight(quest);
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
    }
}