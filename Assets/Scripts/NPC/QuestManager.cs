using System;
using System.Collections.Generic;
using UnityEngine;
using SimpleSurvival.Items;
using SimpleSurvival.Player;
using SimpleSurvival.Stats;
using SimpleSurvival.UI.Hud;

namespace SimpleSurvival.Quests
{
    public sealed class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        [SerializeField] private PlayerInventoryQueries inventoryQueries;

        private readonly Dictionary<QuestData, QuestProgress> _activeQuests = new Dictionary<QuestData, QuestProgress>();
        private readonly HashSet<QuestData> _completedQuests = new HashSet<QuestData>();

        public event Action<QuestData> OnQuestStarted;
        public event Action<QuestData, int> OnObjectiveProgress;
        public event Action<QuestData> OnQuestReadyToTurnIn;
        public event Action<QuestData> OnQuestCompleted;

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
            OnQuestCompleted?.Invoke(quest);
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
            foreach (var kvp in _activeQuests)
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

            foreach (var kvp in _activeQuests)
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
    }
}