using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleSurvival.Quests
{
    public sealed class QuestRewardPopupUI : MonoBehaviour
    {
        public static QuestRewardPopupUI Instance { get; private set; }

        [Header("Root")]
        [SerializeField] private GameObject panelRoot;

        [Header("Reward Display")]
        [SerializeField] private QuestRewardEntryUI entryPrefab;
        [SerializeField] private Transform entryContainer;

        [Header("Button")]
        [SerializeField] private Button claimButton;

        private QuestData _pendingQuest;
        private readonly List<QuestRewardEntryUI> _spawnedEntries = new List<QuestRewardEntryUI>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (panelRoot != null) panelRoot.SetActive(false);
            if (claimButton != null) claimButton.onClick.AddListener(HandleClaim);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (claimButton != null) claimButton.onClick.RemoveListener(HandleClaim);
        }

        public void Show(QuestData quest)
        {
            if (quest == null || panelRoot == null) return;

            ClearEntries();
            _pendingQuest = quest;

            if (entryPrefab != null && entryContainer != null)
            {
                foreach (var reward in quest.Rewards)
                {
                    QuestRewardEntryUI entry = Instantiate(entryPrefab, entryContainer);
                    entry.SetReward(reward.itemData, reward.quantity);
                    _spawnedEntries.Add(entry);
                }
            }

            panelRoot.SetActive(true);
        }

        private void HandleClaim()
        {
            if (_pendingQuest == null) return;

            QuestManager manager = QuestManager.Instance;
            if (manager != null)
                manager.CompleteQuest(_pendingQuest);

            Hide();
        }

        private void Hide()
        {
            _pendingQuest = null;
            ClearEntries();
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void ClearEntries()
        {
            foreach (var entry in _spawnedEntries)
            {
                if (entry != null) Destroy(entry.gameObject);
            }
            _spawnedEntries.Clear();
        }
    }
}