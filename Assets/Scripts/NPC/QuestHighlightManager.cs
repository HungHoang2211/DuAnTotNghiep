using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleSurvival.Items;
using SimpleSurvival.Stats;
using SimpleSurvival.Player;

namespace SimpleSurvival.Quests
{
    public sealed class QuestHighlightManager : MonoBehaviour
    {
        public static QuestHighlightManager Instance { get; private set; }

        [SerializeField] private float refreshInterval = 0.3f;

        private readonly HashSet<ItemData> _pickupItems = new HashSet<ItemData>();
        private readonly HashSet<ItemData> _harvestItems = new HashSet<ItemData>();
        private readonly HashSet<ItemData> _craftItems = new HashSet<ItemData>();
        private readonly HashSet<EnemyStatsConfig> _enemyConfigs = new HashSet<EnemyStatsConfig>();

        private readonly List<QuestItemHighlight> _itemCandidates = new List<QuestItemHighlight>();
        private readonly List<QuestEnemyHighlight> _enemyCandidates = new List<QuestEnemyHighlight>();

        private QuestData _activeQuest;
        private Coroutine _arbitrationRoutine;

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

            _arbitrationRoutine = StartCoroutine(ArbitrationRoutine());
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;

            QuestManager manager = QuestManager.Instance;
            if (manager != null)
                manager.OnObjectiveProgress -= HandleObjectiveProgress;

            if (_arbitrationRoutine != null)
                StopCoroutine(_arbitrationRoutine);
        }

        public void RegisterItemCandidate(QuestItemHighlight candidate)
        {
            if (!_itemCandidates.Contains(candidate))
                _itemCandidates.Add(candidate);
        }

        public void UnregisterItemCandidate(QuestItemHighlight candidate)
        {
            _itemCandidates.Remove(candidate);
        }

        public void RegisterEnemyCandidate(QuestEnemyHighlight candidate)
        {
            if (!_enemyCandidates.Contains(candidate))
                _enemyCandidates.Add(candidate);
        }

        public void UnregisterEnemyCandidate(QuestEnemyHighlight candidate)
        {
            _enemyCandidates.Remove(candidate);
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

            ArbitrateNearest();
            OnHighlightChanged?.Invoke();
        }

        private IEnumerator ArbitrationRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(refreshInterval);
                ArbitrateNearest();
            }
        }

        private void ArbitrateNearest()
        {
            Transform player = PlayerActionController.Instance != null ? PlayerActionController.Instance.PlayerTransform : null;

            ArbitrateItems(player);
            ArbitrateEnemies(player);
        }

        private void ArbitrateItems(Transform player)
        {
            for (int i = _itemCandidates.Count - 1; i >= 0; i--)
            {
                if (_itemCandidates[i] == null) _itemCandidates.RemoveAt(i);
            }

            HashSet<QuestItemHighlight> winners = new HashSet<QuestItemHighlight>();

            if (player != null)
            {
                foreach (ItemData item in _pickupItems)
                {
                    QuestItemHighlight nearest = FindNearestItem(player, item, isHarvest: false);
                    if (nearest != null) winners.Add(nearest);
                }

                foreach (ItemData item in _harvestItems)
                {
                    QuestItemHighlight nearest = FindNearestItem(player, item, isHarvest: true);
                    if (nearest != null) winners.Add(nearest);
                }
            }

            foreach (var candidate in _itemCandidates)
                candidate.SetHighlighted(winners.Contains(candidate));
        }

        private QuestItemHighlight FindNearestItem(Transform player, ItemData item, bool isHarvest)
        {
            QuestItemHighlight nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var candidate in _itemCandidates)
            {
                bool matches = isHarvest ? candidate.MatchesHarvest(item) : candidate.MatchesPickup(item);
                if (!matches) continue;

                float dist = Vector3.Distance(player.position, candidate.HighlightTransform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = candidate;
                }
            }

            return nearest;
        }

        private void ArbitrateEnemies(Transform player)
        {
            for (int i = _enemyCandidates.Count - 1; i >= 0; i--)
            {
                if (_enemyCandidates[i] == null) _enemyCandidates.RemoveAt(i);
            }

            HashSet<QuestEnemyHighlight> winners = new HashSet<QuestEnemyHighlight>();

            if (player != null)
            {
                foreach (EnemyStatsConfig config in _enemyConfigs)
                {
                    QuestEnemyHighlight nearest = FindNearestEnemy(player, config);
                    if (nearest != null) winners.Add(nearest);
                }
            }

            foreach (var candidate in _enemyCandidates)
                candidate.SetHighlighted(winners.Contains(candidate));
        }

        private QuestEnemyHighlight FindNearestEnemy(Transform player, EnemyStatsConfig config)
        {
            QuestEnemyHighlight nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var candidate in _enemyCandidates)
            {
                if (candidate.EnemyConfig != config) continue;

                float dist = Vector3.Distance(player.position, candidate.HighlightTransform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = candidate;
                }
            }

            return nearest;
        }
    }
}