using UnityEngine;
using SimpleSurvival.UI.Hud;
using SimpleSurvival.Quests;

namespace SimpleSurvival.AI
{
    /// <summary>
    /// Luồng: (1) player tương tác lần 1 trong lúc findQuest đang active -> auto hoàn thành
    /// + nhận thưởng. (2) tương tác lần 2 -> Emily giao escortQuest và bắt đầu di chuyển
    /// tới EscortPoint. (3) trong lúc hộ tống nếu bị đánh trúng -> phản đòn animation ngẫu nhiên.
    /// (4) chết trong lúc hộ tống -> fail quest (ragdoll qua NPCEmilyRagdollController).
    /// (5) tới nơi -> complete quest.
    /// </summary>
    public sealed class NPCEmilyController : BaseNPCController
    {
        [Header("Định danh (khớp targetNpcId trong QuestObjectiveData của MrBeat)")]
        [SerializeField] private string npcId = "emily";

        [Header("Quest MrBeat giao - tìm Emily (objective type FindNPC)")]
        [SerializeField] private QuestData findQuest;

        [Header("Quest Emily giao - hộ tống (objective type EscortNPC)")]
        [SerializeField] private QuestData escortQuest;

        [Header("Dialogue")]
        [SerializeField] private string escortOfferDialogue = "Bạn có thể hộ tống tôi đến nơi đó không?";
        [SerializeField] private string escortInProgressDialogue = "Đi thôi, tôi sẽ theo sau bạn.";
        [SerializeField] private string escortDoneDialogue = "Cảm ơn bạn rất nhiều!";
        [SerializeField] private string allDoneDialogue = "Tôi ổn rồi, cảm ơn bạn.";

        [Header("Refs")]
        [SerializeField] private NPCEmilyMovement movement;
        [SerializeField] private NPCEmilyStats stats;
        [SerializeField] private NPCEmilyAnimator animatorController;
        [SerializeField] private EscortEnemyDirector enemyDirector;

        public bool IsEscorting { get; private set; }

        protected override void Start()
        {
            base.Start();

            var manager = QuestManager.Instance;
            if (manager != null)
                manager.OnQuestFailed += HandleQuestFailed;

            if (stats != null)
            {
                stats.OnDeath += HandleDeath;
                stats.OnDamagedBy += HandleDamaged;
            }
        }

        private void OnDestroy()
        {
            var manager = QuestManager.Instance;
            if (manager != null)
                manager.OnQuestFailed -= HandleQuestFailed;

            if (stats != null)
            {
                stats.OnDeath -= HandleDeath;
                stats.OnDamagedBy -= HandleDamaged;
            }
        }

        public override void OnPlayerInteract(GameObject player)
        {
            var manager = QuestManager.Instance;
            if (manager == null) return;

            if (findQuest != null && manager.IsQuestActive(findQuest))
            {
                manager.NotifyNPCFound(npcId);
                if (manager.IsReadyToTurnIn(findQuest))
                {
                    manager.CompleteQuest(findQuest);
                    ShowDialogue(findQuest.TurnInDialogue);
                }
                return;
            }

            bool escortNotStartedYet = escortQuest != null
                && !manager.IsQuestActive(escortQuest)
                && !manager.IsQuestCompleted(escortQuest)
                && !IsEscorting;

            if (escortNotStartedYet)
            {
                ShowDialogue(escortOfferDialogue);
                manager.StartQuest(escortQuest);
                BeginEscort();
                return;
            }

            if (IsEscorting)
            {
                ShowDialogue(escortInProgressDialogue);
                return;
            }

            if (escortQuest != null && manager.IsQuestCompleted(escortQuest))
                ShowDialogue(allDoneDialogue);
        }

        private void BeginEscort()
        {
            if (escortQuest == null) return;

            string pointId = null;
            foreach (var objective in escortQuest.Objectives)
            {
                if (objective.type == QuestObjectiveType.EscortNPC)
                {
                    pointId = objective.escortPointId;
                    break;
                }
            }

            EscortPoint point = EscortPoint.Find(pointId);
            if (point == null)
            {
                Debug.LogError($"[NPCEmilyController] Không tìm thấy EscortPoint id '{pointId}'", this);
                return;
            }

            IsEscorting = true;
            movement?.BeginMoveTo(point.Position);
            enemyDirector?.BeginEncounter(transform);
        }

        private void Update()
        {
            if (!IsEscorting) return;
            if (movement == null || !movement.HasArrived) return;

            IsEscorting = false;
            enemyDirector?.StopEncounter();
            QuestManager.Instance?.NotifyEscortArrived(escortQuest);
            ShowDialogue(escortDoneDialogue);
        }

        private void HandleDamaged(GameObject attacker)
        {
            if (!IsEscorting) return;
            animatorController?.TriggerRandomAttack();
        }

        private void HandleDeath(GameObject source)
        {
            if (!IsEscorting) return;

            IsEscorting = false;
            movement?.Stop();
            enemyDirector?.StopEncounter();
            QuestManager.Instance?.FailQuest(escortQuest);
        }

        private void HandleQuestFailed(QuestData quest)
        {
            if (quest != escortQuest) return;
            IsEscorting = false;
            movement?.Stop();
            enemyDirector?.StopEncounter();
        }

        private void ShowDialogue(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            HudManager hud = HudManager.Instance;
            if (hud != null && hud.Speech != null)
                hud.Speech.Show(transform, text, SpeechHudType.Neutral);
        }
    }
}