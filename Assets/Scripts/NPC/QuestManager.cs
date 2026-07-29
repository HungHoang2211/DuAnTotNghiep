using System;
using System.Collections.Generic;
using UnityEngine;
using SimpleSurvival.Items;
using SimpleSurvival.Player;
using SimpleSurvival.SaveLoad;
using SimpleSurvival.Stats;
using SimpleSurvival.UI.Hud;

namespace SimpleSurvival.Quests
{
    public sealed class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        [SerializeField] private PlayerInventoryQueries inventoryQueries;
        [SerializeField] private QuestDatabase questDatabase;

        private readonly Dictionary<QuestData, QuestProgress> _activeQuests = new Dictionary<QuestData, QuestProgress>();
        private readonly HashSet<QuestData> _completedQuests = new HashSet<QuestData>();
        private readonly HashSet<string> _permanentlyLockedMaps = new HashSet<string>();

        public bool StoryCompleted { get; private set; }

        public event Action<QuestData> OnQuestStarted;
        public event Action<QuestData, int> OnObjectiveProgress;
        public event Action<QuestData> OnQuestReadyToTurnIn;
        public event Action<QuestData> OnQuestCompleted;
        public event Action<QuestData> OnQuestFailed;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (inventoryQueries != null)
                inventoryQueries.OnItemAdded += HandleItemAdded;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (inventoryQueries != null)
                inventoryQueries.OnItemAdded -= HandleItemAdded;
        }

        public bool IsQuestActive(QuestData quest) => quest != null && _activeQuests.ContainsKey(quest);

        public bool IsQuestCompleted(QuestData quest) => quest != null && _completedQuests.Contains(quest);

        public bool IsReadyToTurnIn(QuestData quest)
        {
            return _activeQuests.TryGetValue(quest, out QuestProgress progress) && progress.IsAllComplete();
        }

        public int GetObjectiveProgress(QuestData quest, int objectiveIndex)
        {
            return _activeQuests.TryGetValue(quest, out QuestProgress progress) ? progress.GetAmount(objectiveIndex) : 0;
        }

        public bool IsMapPermanentlyLocked(string mapId) => !string.IsNullOrEmpty(mapId) && _permanentlyLockedMaps.Contains(mapId);

        public void LockMapPermanently(string mapId)
        {
            if (string.IsNullOrEmpty(mapId)) return;
            _permanentlyLockedMaps.Add(mapId);
        }

        public void StartQuest(QuestData quest)
        {
            if (quest == null) return;
            if (_activeQuests.ContainsKey(quest)) return;
            if (_completedQuests.Contains(quest)) return;

            _activeQuests[quest] = new QuestProgress(quest);
            OnQuestStarted?.Invoke(quest);
        }

        public void CompleteQuest(QuestData quest)
        {
            if (quest == null) return;
            if (!_activeQuests.TryGetValue(quest, out QuestProgress progress)) return;
            if (!progress.IsAllComplete()) return;

            GrantRewards(quest);

            _activeQuests.Remove(quest);
            _completedQuests.Add(quest);

            if (quest.MarksStoryComplete)
            {
                StoryCompleted = true;
            }

            OnQuestCompleted?.Invoke(quest);
        }

        public void FailQuest(QuestData quest)
        {
            if (quest == null) return;
            if (!_activeQuests.ContainsKey(quest)) return;

            _activeQuests.Remove(quest);
            OnQuestFailed?.Invoke(quest);
        }

        private void GrantRewards(QuestData quest)
        {
            if (inventoryQueries == null) return;

            foreach (var reward in quest.Rewards)
            {
                if (reward.itemData == null || reward.quantity <= 0) continue;

                inventoryQueries.AddItem(reward.itemData, reward.quantity);

                if (FollowNotifyManager.Instance != null)
                    FollowNotifyManager.Instance.Notify($"+{reward.quantity} {reward.itemData.ItemName}", SpeechHudType.Good);
            }
        }

        private void HandleItemAdded(ItemData itemData, int amount)
        {
            var snapshot = new List<KeyValuePair<QuestData, QuestProgress>>(_activeQuests);
            foreach (var kvp in snapshot)
            {
                QuestData quest = kvp.Key;
                QuestProgress progress = kvp.Value;
                bool changed = false;

                for (int i = 0; i < quest.Objectives.Count; i++)
                {
                    QuestObjectiveData objective = quest.Objectives[i];
                    if (objective.type != QuestObjectiveType.CollectItem) continue;
                    if (objective.targetItem != itemData) continue;
                    if (progress.IsObjectiveComplete(i)) continue;

                    progress.AddProgress(i, amount);
                    OnObjectiveProgress?.Invoke(quest, i);
                    changed = true;
                }

                if (changed && progress.IsAllComplete())
                    OnQuestReadyToTurnIn?.Invoke(quest);
            }
        }
        public void NotifyEnemyKilled(EnemyStatsConfig enemyConfig)
        {
            if (enemyConfig == null) return;

            var snapshot = new List<KeyValuePair<QuestData, QuestProgress>>(_activeQuests);
            foreach (var kvp in snapshot)
            {
                QuestData quest = kvp.Key;
                QuestProgress progress = kvp.Value;
                bool changed = false;

                for (int i = 0; i < quest.Objectives.Count; i++)
                {
                    QuestObjectiveData objective = quest.Objectives[i];
                    if (objective.type != QuestObjectiveType.KillEnemy) continue;
                    if (objective.targetEnemyConfig != enemyConfig) continue;
                    if (progress.IsObjectiveComplete(i)) continue;

                    progress.AddProgress(i, 1);
                    OnObjectiveProgress?.Invoke(quest, i);
                    changed = true;
                }

                if (changed && progress.IsAllComplete())
                    OnQuestReadyToTurnIn?.Invoke(quest);
            }
        }
        public void NotifyHarvestCompleted(ItemData itemData)
        {
            if (itemData == null) return;

            var snapshot = new List<KeyValuePair<QuestData, QuestProgress>>(_activeQuests);
            foreach (var kvp in snapshot)
            {
                QuestData quest = kvp.Key;
                QuestProgress progress = kvp.Value;
                bool changed = false;

                for (int i = 0; i < quest.Objectives.Count; i++)
                {
                    QuestObjectiveData objective = quest.Objectives[i];
                    if (objective.type != QuestObjectiveType.HarvestNode) continue;
                    if (objective.targetItem != itemData) continue;
                    if (progress.IsObjectiveComplete(i)) continue;

                    progress.AddProgress(i, 1);
                    OnObjectiveProgress?.Invoke(quest, i);
                    changed = true;
                }

                if (changed && progress.IsAllComplete())
                    OnQuestReadyToTurnIn?.Invoke(quest);
            }
        }

        public void NotifyItemCrafted(ItemData itemData)
        {
            if (itemData == null) return;

            var snapshot = new List<KeyValuePair<QuestData, QuestProgress>>(_activeQuests);
            foreach (var kvp in snapshot)
            {
                QuestData quest = kvp.Key;
                QuestProgress progress = kvp.Value;
                bool changed = false;

                for (int i = 0; i < quest.Objectives.Count; i++)
                {
                    QuestObjectiveData objective = quest.Objectives[i];
                    if (objective.type != QuestObjectiveType.CraftItem) continue;
                    if (objective.targetItem != itemData) continue;
                    if (progress.IsObjectiveComplete(i)) continue;

                    progress.AddProgress(i, 1);
                    OnObjectiveProgress?.Invoke(quest, i);
                    changed = true;
                }

                if (changed && progress.IsAllComplete())
                    OnQuestReadyToTurnIn?.Invoke(quest);
            }
        }
        public void NotifyNPCFound(string npcId)
        {
            if (string.IsNullOrEmpty(npcId)) return;

            var snapshot = new List<KeyValuePair<QuestData, QuestProgress>>(_activeQuests);
            foreach (var kvp in snapshot)
            {
                QuestData quest = kvp.Key;
                QuestProgress progress = kvp.Value;
                bool changed = false;

                for (int i = 0; i < quest.Objectives.Count; i++)
                {
                    QuestObjectiveData objective = quest.Objectives[i];
                    if (objective.type != QuestObjectiveType.FindNPC) continue;
                    if (objective.targetNpcId != npcId) continue;
                    if (progress.IsObjectiveComplete(i)) continue;

                    progress.AddProgress(i, objective.requiredAmount);
                    OnObjectiveProgress?.Invoke(quest, i);
                    changed = true;
                }

                if (changed && progress.IsAllComplete())
                    OnQuestReadyToTurnIn?.Invoke(quest);
            }
        }

        public void NotifyEscortArrived(QuestData quest)
        {
            if (quest == null) return;
            if (!_activeQuests.TryGetValue(quest, out QuestProgress progress)) return;

            for (int i = 0; i < quest.Objectives.Count; i++)
            {
                QuestObjectiveData objective = quest.Objectives[i];
                if (objective.type != QuestObjectiveType.EscortNPC) continue;
                if (progress.IsObjectiveComplete(i)) continue;

                progress.AddProgress(i, objective.requiredAmount);
                OnObjectiveProgress?.Invoke(quest, i);
            }

            if (progress.IsAllComplete())
                CompleteQuest(quest);
        }

        public void NotifyTowerRepaired(string towerId)
        {
            if (string.IsNullOrEmpty(towerId)) return;

            var snapshot = new List<KeyValuePair<QuestData, QuestProgress>>(_activeQuests);
            foreach (var kvp in snapshot)
            {
                QuestData quest = kvp.Key;
                QuestProgress progress = kvp.Value;
                bool changed = false;

                for (int i = 0; i < quest.Objectives.Count; i++)
                {
                    QuestObjectiveData objective = quest.Objectives[i];
                    if (objective.type != QuestObjectiveType.RepairTower) continue;
                    if (objective.targetTowerId != towerId) continue;
                    if (progress.IsObjectiveComplete(i)) continue;

                    progress.AddProgress(i, objective.requiredAmount);
                    OnObjectiveProgress?.Invoke(quest, i);
                    changed = true;
                }

                if (changed && progress.IsAllComplete())
                    CompleteQuest(quest);
            }
        }

        public WorldData Capture()
        {
            WorldData data = new WorldData
            {
                storyCompleted = StoryCompleted,
                permanentlyLockedMapIds = new List<string>(_permanentlyLockedMaps)
            };

            foreach (QuestData quest in _completedQuests)
            {
                if (string.IsNullOrWhiteSpace(quest.QuestId)) continue;
                data.completedQuestIds.Add(quest.QuestId);
            }

            foreach (var kvp in _activeQuests)
            {
                QuestData quest = kvp.Key;
                if (string.IsNullOrWhiteSpace(quest.QuestId)) continue;

                QuestProgress progress = kvp.Value;
                ActiveQuestData activeData = new ActiveQuestData { questId = quest.QuestId };
                for (int i = 0; i < quest.Objectives.Count; i++)
                    activeData.objectiveProgress.Add(progress.GetAmount(i));

                data.activeQuests.Add(activeData);
            }

            return data;
        }
        public void DebugForceCompleteQuest(QuestData quest)
        {
            if (quest == null) return;

            if (!_activeQuests.ContainsKey(quest))
                StartQuest(quest);

            if (_activeQuests.TryGetValue(quest, out QuestProgress progress))
            {
                for (int i = 0; i < quest.Objectives.Count; i++)
                    progress.SetAmount(i, quest.Objectives[i].requiredAmount);
            }

            CompleteQuest(quest);
        }
        public void Restore(WorldData data)
        {
            _activeQuests.Clear();
            _completedQuests.Clear();
            _permanentlyLockedMaps.Clear();
            StoryCompleted = false;

            if (data == null || questDatabase == null)
                return;

            StoryCompleted = data.storyCompleted;

            if (data.permanentlyLockedMapIds != null)
                foreach (string mapId in data.permanentlyLockedMapIds)
                    _permanentlyLockedMaps.Add(mapId);

            if (data.completedQuestIds != null)
            {
                foreach (string questId in data.completedQuestIds)
                {
                    if (questDatabase.TryGet(questId, out QuestData quest))
                        _completedQuests.Add(quest);
                }
            }

            if (data.activeQuests != null)
            {
                foreach (ActiveQuestData activeData in data.activeQuests)
                {
                    if (!questDatabase.TryGet(activeData.questId, out QuestData quest))
                        continue;

                    QuestProgress progress = new QuestProgress(quest);
                    for (int i = 0; i < activeData.objectiveProgress.Count && i < quest.Objectives.Count; i++)
                        progress.SetAmount(i, activeData.objectiveProgress[i]);

                    _activeQuests[quest] = progress;
                }
            }
        }
    }
}