using System.Collections.Generic;
using UnityEngine;
using SimpleSurvival.UI.Hud;
using SimpleSurvival.Quests;
using SimpleSurvival.Progression;

namespace SimpleSurvival.AI
{
    public sealed class NPCQuestGiver : BaseNPCController
    {
        [Header("Quest Chain")]
        [SerializeField] private List<QuestData> questChain = new List<QuestData>();

        [Header("Quest In Progress")]
        [SerializeField] private string questInProgressDialogue = "Nhiệm vụ vẫn chưa xong đâu, cố lên nhé!";

        [Header("No Quest Left")]
        [SerializeField] private string noQuestsAvailableDialogue = "Tôi không còn việc gì để nhờ bạn nữa.";

        [Header("Locked By Level")]
        [SerializeField] private string lockedByLevelDialogue = "Bạn cần lên cấp cao hơn để nhận nhiệm vụ này.";

        [Header("Refs")]
        [SerializeField] private NPCQuestIndicator indicator;
        [SerializeField] private GameObject groundHighlight;

        protected override void Start()
        {
            base.Start();

            var manager = QuestManager.Instance;
            if (manager != null)
            {
                manager.OnObjectiveProgress += HandleProgressChanged;
                manager.OnQuestCompleted += HandleQuestCompleted;
            }

            if (PlayerLevelSystem.Instance != null)
                PlayerLevelSystem.Instance.OnLevelUp += HandleLevelUp;

            RefreshIndicator();
        }

        private void OnDestroy()
        {
            var manager = QuestManager.Instance;
            if (manager != null)
            {
                manager.OnObjectiveProgress -= HandleProgressChanged;
                manager.OnQuestCompleted -= HandleQuestCompleted;
            }

            if (PlayerLevelSystem.Instance != null)
                PlayerLevelSystem.Instance.OnLevelUp -= HandleLevelUp;
        }

        public override void OnPlayerInteract(GameObject player)
        {
            var manager = QuestManager.Instance;
            if (manager == null) return;

            QuestData currentQuest = GetCurrentQuest(manager);

            if (currentQuest == null)
            {
                ShowDialogue(noQuestsAvailableDialogue);
                return;
            }

            if (manager.IsQuestActive(currentQuest))
            {
                if (manager.IsReadyToTurnIn(currentQuest))
                {
                    ShowDialogue(currentQuest.TurnInDialogue);
                    manager.CompleteQuest(currentQuest);
                    RefreshIndicator();
                }
                else
                {
                    ShowDialogue(questInProgressDialogue);
                }
                return;
            }

            if (!IsLevelMet(currentQuest))
            {
                ShowDialogue(lockedByLevelDialogue);
                return;
            }

            ShowDialogue(currentQuest.OfferDialogue);
            manager.StartQuest(currentQuest);
            RefreshIndicator();
        }

        private QuestData GetCurrentQuest(QuestManager manager)
        {
            foreach (var quest in questChain)
            {
                if (quest == null) continue;
                if (!manager.IsQuestCompleted(quest)) return quest;
            }
            return null;
        }

        private bool IsLevelMet(QuestData quest)
        {
            return PlayerLevelSystem.Instance == null || PlayerLevelSystem.Instance.HasReachedLevel(quest.RequiredLevel);
        }

        private void ShowDialogue(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            HudManager hud = HudManager.Instance;
            if (hud != null && hud.Speech != null)
                hud.Speech.Show(transform, text, SpeechHudType.Neutral);
        }

        private void RefreshIndicator()
        {
            var manager = QuestManager.Instance;
            if (manager == null)
            {
                indicator?.Hide();
                SetGroundHighlight(false);
                return;
            }

            QuestData currentQuest = GetCurrentQuest(manager);

            if (currentQuest == null)
            {
                indicator?.Hide();
                SetGroundHighlight(false);
                return;
            }

            if (manager.IsQuestActive(currentQuest))
            {
                bool readyToTurnIn = manager.IsReadyToTurnIn(currentQuest);
                indicator?.SetState(readyToTurnIn ? NPCQuestState.ReadyToTurnIn : NPCQuestState.InProgress);
                SetGroundHighlight(readyToTurnIn);
                return;
            }

            bool levelMet = IsLevelMet(currentQuest);
            indicator?.SetState(levelMet ? NPCQuestState.Available : NPCQuestState.NoQuest);
            SetGroundHighlight(levelMet);
        }

        private void SetGroundHighlight(bool value)
        {
            if (groundHighlight != null) groundHighlight.SetActive(value);
        }

        private void HandleProgressChanged(QuestData quest, int objectiveIndex)
        {
            if (!questChain.Contains(quest)) return;
            RefreshIndicator();
        }

        private void HandleQuestCompleted(QuestData quest)
        {
            if (!questChain.Contains(quest)) return;
            RefreshIndicator();
        }

        private void HandleLevelUp(int newLevel)
        {
            RefreshIndicator();
        }
    }
}