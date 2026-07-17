using System.Collections;
using UnityEngine;
using SimpleSurvival.Stats;
using SimpleSurvival.UI.Hud;
using SimpleSurvival.Quests;

namespace SimpleSurvival.AI
{
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

        [Header("Phản đòn")]
        [Tooltip("Khoảng cách giữa mỗi lần chơi animation attack trong lúc đang đứng đánh nhau")]
        [SerializeField] private float attackInterval = 1.2f;
        [Tooltip("Damage Emily gây ra mỗi đòn phản đòn trúng")]
        [SerializeField] private float attackDamage = 10f;

        [Header("Refs")]
        [SerializeField] private NPCEmilyMovement movement;
        [SerializeField] private NPCEmilyStats stats;
        [SerializeField] private NPCEmilyAnimator animatorController;
        [SerializeField] private EscortEnemyDirector enemyDirector;

        public bool IsEscorting { get; private set; }

        private GameObject _currentThreat;
        private BaseStats _currentThreatStats;
        private Coroutine _combatRoutine;

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

            if (animatorController != null)
                animatorController.OnAttackHit += HandleAttackHit;
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

            if (animatorController != null)
                animatorController.OnAttackHit -= HandleAttackHit;

            ExitCombat();
        }

        public override void OnPlayerInteract(GameObject player)
        {
            var manager = QuestManager.Instance;
            if (manager == null) return;

            // 1) Quest "tìm Emily" đang active -> báo tìm thấy + nhận thưởng ngay
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

            // 2) Quest hộ tống ĐÃ được giao nhưng CHƯA khởi hành -> lần tương tác này mới thật sự đi.
            //    (Emily đứng yên, giữ lời mời trên đầu, kể từ lúc quest được giao ở bước 3 bên dưới
            //    cho tới khi player bấm tương tác thêm 1 lần nữa thì mới BeginEscort()).
            if (escortQuest != null && manager.IsQuestActive(escortQuest) && !IsEscorting)
            {
                ShowDialogue(escortInProgressDialogue);
                BeginEscort();
                return;
            }

            // 3) Đã tìm xong, hộ tống chưa từng được giao -> giao quest, hiện lời mời, ĐỨNG YÊN
            //    (không gọi BeginEscort() ở đây, chờ tương tác lần nữa ở bước 2 phía trên)
            bool escortNotOfferedYet = escortQuest != null
                && !manager.IsQuestActive(escortQuest)
                && !manager.IsQuestCompleted(escortQuest);

            if (escortNotOfferedYet)
            {
                ShowDialogue(escortOfferDialogue);
                manager.StartQuest(escortQuest);
                return;
            }

            // 4) Đang hộ tống (đã khởi hành)
            if (IsEscorting)
            {
                ShowDialogue(escortInProgressDialogue);
                return;
            }

            // 5) Đã hoàn thành hết
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
            if (!IsEscorting || attacker == null) return;

            // Đã đang đánh nhau với đúng kẻ này rồi -> chỉ cập nhật hướng, không khởi động lại
            if (_currentThreat == attacker)
            {
                FaceAttacker(attacker.transform);
                return;
            }

            EnterCombat(attacker);
        }

        private void EnterCombat(GameObject attacker)
        {
            // Nếu đang đánh nhau với 1 kẻ khác thì gỡ đăng ký cũ trước khi đổi mục tiêu
            if (_currentThreatStats != null)
                _currentThreatStats.OnDeath -= HandleThreatDefeated;

            _currentThreat = attacker;
            _currentThreatStats = attacker.GetComponent<BaseStats>();

            FaceAttacker(attacker.transform);
            movement?.SetPaused(true);

            if (_currentThreatStats != null)
                _currentThreatStats.OnDeath += HandleThreatDefeated;

            if (_combatRoutine != null) StopCoroutine(_combatRoutine);
            _combatRoutine = StartCoroutine(CombatLoop());
        }

        private IEnumerator CombatLoop()
        {
            while (_currentThreat != null)
            {
                animatorController?.TriggerRandomAttack();
                yield return new WaitForSeconds(attackInterval);
                if (_currentThreat != null) FaceAttacker(_currentThreat.transform);
            }
        }

        /// <summary>
        /// Gọi từ animation event (qua NPCEmilyAnimator.OnAttackHit) đúng lúc tay Emily
        /// chạm mục tiêu trong animation attack - đây là nơi thực sự áp damage,
        /// giống PlayerAnimationRelay.OnAttackHit() gọi attack.HandleHit().
        /// </summary>
        private void HandleAttackHit()
        {
            if (_currentThreat == null || _currentThreatStats == null) return;
            _currentThreatStats.TakeDamage(attackDamage, gameObject);
        }

        private void HandleThreatDefeated(GameObject source)
        {
            ExitCombat();
        }

        /// <summary>
        /// Kết thúc trạng thái đứng đánh nhau: gỡ đăng ký, dừng animation loop, cho di chuyển tiếp.
        /// </summary>
        private void ExitCombat()
        {
            if (_currentThreatStats != null)
            {
                _currentThreatStats.OnDeath -= HandleThreatDefeated;
                _currentThreatStats = null;
            }
            _currentThreat = null;

            if (_combatRoutine != null)
            {
                StopCoroutine(_combatRoutine);
                _combatRoutine = null;
            }

            movement?.SetPaused(false);
        }

        private void FaceAttacker(Transform attacker)
        {
            if (attacker == null) return;
            Vector3 dir = attacker.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.LookRotation(dir);
        }

        private void HandleDeath(GameObject source)
        {
            // Luôn dừng hẳn di chuyển + phản đòn khi Emily chết, bất kể đang hộ tống hay không
            ExitCombat();
            movement?.Stop();

            if (!IsEscorting) return;

            IsEscorting = false;
            enemyDirector?.StopEncounter();
            QuestManager.Instance?.FailQuest(escortQuest);
        }

        private void HandleQuestFailed(QuestData quest)
        {
            if (quest != escortQuest) return;
            IsEscorting = false;
            ExitCombat();
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