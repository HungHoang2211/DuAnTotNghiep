using System;
using System.Collections.Generic;
using UnityEngine;
using SimpleSurvival.Items;
using SimpleSurvival.Stats;

namespace SimpleSurvival.Quests
{
    public sealed class QuestHighlightManager : MonoBehaviour
    {
        public static QuestHighlightManager Instance { get; private set; }

        private readonly HashSet<ItemData> _pickupItems = new HashSet<ItemData>();
        private readonly HashSet<ItemData> _harvestItems = new HashSet<ItemData>();
        private readonly HashSet<ItemData> _craftItems = new HashSet<ItemData>();
        private readonly HashSet<EnemyStatsConfig> _enemyConfigs = new HashSet<EnemyStatsConfig>();

        private QuestData _activeQuest;

        public event Action OnHighlightChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            QuestManager manager = QuestManager.Instance;
            if (manager != null)
                manager.OnObjectiveProgress += HandleObjectiveProgress;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;

            QuestManager manager = QuestManager.Instance;
            if (manager != null)
                manager.OnObjectiveProgress -= HandleObjectiveProgress;
        }

        public void SetActiveQuest(QuestData quest)
        {
            _activeQuest = quest;
            RebuildHighlightSets();
        }

        public void ClearActiveQuest()
        {
            _activeQuest = null;
            RebuildHighlightSets();
        }

        public bool IsItemPickupHighlighted(ItemData item) => item != null && _pickupItems.Contains(item);
        public bool IsItemHarvestHighlighted(ItemData item) => item != null && _harvestItems.Contains(item);
        public bool IsItemCraftHighlighted(ItemData item) => item != null && _craftItems.Contains(item);
        public bool IsEnemyHighlighted(EnemyStatsConfig config) => config != null && _enemyConfigs.Contains(config);

        private void HandleObjectiveProgress(QuestData quest, int objectiveIndex)
        {
            if (quest != _activeQuest) return;
            RebuildHighlightSets();
        }

        private void RebuildHighlightSets()
        {
            _pickupItems.Clear();
            _harvestItems.Clear();
            _craftItems.Clear();
            _enemyConfigs.Clear();

            QuestManager manager = QuestManager.Instance;
            if (manager != null && _activeQuest != null)
            {
                for (int i = 0; i < _activeQuest.Objectives.Count; i++)
                {
                    QuestObjectiveData objective = _activeQuest.Objectives[i];
                    if (manager.GetObjectiveProgress(_activeQuest, i) >= objective.requiredAmount) continue;

                    switch (objective.type)
                    {
                        case QuestObjectiveType.CollectItem:
                            if (objective.targetItem != null) _pickupItems.Add(objective.targetItem);
                            break;
                        case QuestObjectiveType.HarvestNode:
                            if (objective.targetItem != null) _harvestItems.Add(objective.targetItem);
                            break;
                        case QuestObjectiveType.CraftItem:
                            if (objective.targetItem != null) _craftItems.Add(objective.targetItem);
                            break;
                        case QuestObjectiveType.KillEnemy:
                            if (objective.targetEnemyConfig != null) _enemyConfigs.Add(objective.targetEnemyConfig);
                            break;
                    }
                }
            }

            OnHighlightChanged?.Invoke();
        }
    }
}