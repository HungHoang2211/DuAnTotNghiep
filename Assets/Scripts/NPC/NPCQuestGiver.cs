using System.Collections.Generic;
using UnityEngine;
using SimpleSurvival.UI.Hud;
using SimpleSurvival.Quests;

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

        [Header("Refs")]
        [SerializeField] private NPCQuestIndicator indicator;

        protected override void Start()
        {
            base.Start();

            var manager = QuestManager.Instance;
            if (manager != null)
            {
                manager.OnObjectiveProgress += HandleProgressChanged;
                manager.OnQuestCompleted += HandleQuestCompleted;
            }

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

        private void ShowDialogue(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            HudManager hud = HudManager.Instance;
            if (hud != null && hud.Speech != null)
                hud.Speech.Show(transform, text, SpeechHudType.Neutral);
        }

        private void RefreshIndicator()
        {
            if (indicator == null) return;

            var manager = QuestManager.Instance;
            if (manager == null)
            {
                indicator.Hide();
                return;
            }

            QuestData currentQuest = GetCurrentQuest(manager);

            if (currentQuest == null)
            {
                indicator.Hide();
                return;
            }

            if (manager.IsQuestActive(currentQuest))
                indicator.SetState(manager.IsReadyToTurnIn(currentQuest)
                    ? NPCQuestState.ReadyToTurnIn
                    : NPCQuestState.InProgress);
            else
                indicator.SetState(NPCQuestState.Available);
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
    }
}