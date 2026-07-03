using UnityEngine;
using SimpleSurvival.UI.Hud;

namespace SimpleSurvival.AI
{
    public sealed class NPCQuestGiver : BaseNPCController
    {
        [Header("Quest")]
        [SerializeField] private SimpleSurvival.Quests.QuestData questData;

        [Header("Refs")]
        [SerializeField] private NPCQuestIndicator indicator;

        protected override void Start()
        {
            base.Start();

            var manager = SimpleSurvival.Quests.QuestManager.Instance;
            if (manager != null)
            {
                manager.OnObjectiveProgress += HandleProgressChanged;
                manager.OnQuestCompleted += HandleQuestCompleted;
            }

            RefreshIndicator();
        }

        private void OnDestroy()
        {
            var manager = SimpleSurvival.Quests.QuestManager.Instance;
            if (manager != null)
            {
                manager.OnObjectiveProgress -= HandleProgressChanged;
                manager.OnQuestCompleted -= HandleQuestCompleted;
            }
        }

        public override void OnPlayerInteract(GameObject player)
        {
            if (questData == null) return;

            var manager = SimpleSurvival.Quests.QuestManager.Instance;
            if (manager == null) return;

            if (manager.IsQuestCompleted(questData)) return;

            if (manager.IsQuestActive(questData))
            {
                if (manager.IsReadyToTurnIn(questData))
                {
                    ShowDialogue(questData.TurnInDialogue);
                    var popup = SimpleSurvival.Quests.QuestRewardPopupUI.Instance;
                    if (popup != null) popup.Show(questData);
                }
                return;
            }

            ShowDialogue(questData.OfferDialogue);
            manager.StartQuest(questData);
            RefreshIndicator();
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
            if (indicator == null || questData == null) return;

            var manager = SimpleSurvival.Quests.QuestManager.Instance;
            if (manager == null)
            {
                indicator.Hide();
                return;
            }

            if (manager.IsQuestCompleted(questData))
                indicator.Hide();
            else if (manager.IsQuestActive(questData))
                indicator.SetState(manager.IsReadyToTurnIn(questData)
                    ? SimpleSurvival.Quests.NPCQuestState.ReadyToTurnIn
                    : SimpleSurvival.Quests.NPCQuestState.InProgress);
            else
                indicator.SetState(SimpleSurvival.Quests.NPCQuestState.Available);
        }

        private void HandleProgressChanged(SimpleSurvival.Quests.QuestData quest, int objectiveIndex)
        {
            if (quest != questData) return;
            RefreshIndicator();
        }

        private void HandleQuestCompleted(SimpleSurvival.Quests.QuestData quest)
        {
            if (quest != questData) return;
            RefreshIndicator();
        }
    }
}