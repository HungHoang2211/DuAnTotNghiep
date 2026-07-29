using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleSurvival.Stats;
using SimpleSurvival.UI.Hud;
using SimpleSurvival.Quests;
using SimpleSurvival.Loot;
using SimpleSurvival.Progression;

namespace SimpleSurvival.AI
{
    public sealed class NPCEmilyController : BaseNPCController
    {
        [Header("Định danh")]
        [SerializeField] private string npcId = "emily";

        [Header("Quest MrBeat")]
        [SerializeField] private QuestData findQuest;

        [Header("Quest Emily")]
        [SerializeField] private QuestData escortQuest;

        [Header("Dialogue")]
        [SerializeField] private string escortOfferDialogue = "Bạn có thể hộ tống tôi đến nơi đó không?";
        [SerializeField] private string escortInProgressDialogue = "Đi thôi, tôi sẽ theo sau bạn.";
        [SerializeField] private string escortDoneDialogue = "Cảm ơn bạn rất nhiều!";
        [SerializeField] private string allDoneDialogue = "Tôi ổn rồi, cảm ơn bạn.";
        [SerializeField] private string lockedByLevelDialogue = "Bạn cần lên cấp cao hơn để nhận nhiệm vụ này.";

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
        [SerializeField] private GameObject groundHighlight;

        public bool IsEscorting { get; private set; }

        private readonly List<EscortPoint> _route = new List<EscortPoint>();
        private int _routeIndex;
        private LootContainer _waitingLootContainer;

        private GameObject _currentThreat;
        private BaseStats _currentThreatStats;
        private Coroutine _combatRoutine;

        protected override void Start()
        {
            base.Start();

            var manager = QuestManager.Instance;
            if (manager != null)
            {
                manager.OnQuestFailed += HandleQuestFailed;
                manager.OnQuestStarted += HandleQuestStateChanged;
                manager.OnObjectiveProgress += HandleObjectiveProgress;
                manager.OnQuestCompleted += HandleQuestStateChanged;
            }

            if (PlayerLevelSystem.Instance != null)
                PlayerLevelSystem.Instance.OnLevelUp += HandleLevelUp;

            if (stats != null)
            {
                stats.OnDeath += HandleDeath;
                stats.OnDamagedBy += HandleDamaged;
            }

            if (animatorController != null)
                animatorController.OnAttackHit += HandleAttackHit;

            RefreshGroundHighlight();
        }

        private void OnDestroy()
        {
            var manager = QuestManager.Instance;
            if (manager != null)
            {
                manager.OnQuestFailed -= HandleQuestFailed;
                manager.OnQuestStarted -= HandleQuestStateChanged;
                manager.OnObjectiveProgress -= HandleObjectiveProgress;
                manager.OnQuestCompleted -= HandleQuestStateChanged;
            }

            if (PlayerLevelSystem.Instance != null)
                PlayerLevelSystem.Instance.OnLevelUp -= HandleLevelUp;

            if (stats != null)
            {
                stats.OnDeath -= HandleDeath;
                stats.OnDamagedBy -= HandleDamaged;
            }

            if (animatorController != null)
                animatorController.OnAttackHit -= HandleAttackHit;

            ExitCombat();
            ResetRouteState();
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

            if (escortQuest != null && manager.IsQuestActive(escortQuest) && !IsEscorting)
            {
                ShowDialogue(escortInProgressDialogue);
                BeginEscort();
                return;
            }

            bool escortNotOfferedYet = escortQuest != null
                && !manager.IsQuestActive(escortQuest)
                && !manager.IsQuestCompleted(escortQuest);

            if (escortNotOfferedYet)
            {
                if (!IsLevelMet(escortQuest))
                {
                    ShowDialogue(lockedByLevelDialogue);
                    return;
                }

                ShowDialogue(escortOfferDialogue);
                manager.StartQuest(escortQuest);
                UnlockRouteContainers();
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

        private bool IsLevelMet(QuestData quest)
        {
            return PlayerLevelSystem.Instance == null || PlayerLevelSystem.Instance.HasReachedLevel(quest.RequiredLevel);
        }

        private void RefreshGroundHighlight()
        {
            var manager = QuestManager.Instance;
            if (manager == null) { SetGroundHighlight(false); return; }

            bool isFindTarget = findQuest != null && manager.IsQuestActive(findQuest);

            bool escortOfferable = escortQuest != null
                && !manager.IsQuestActive(escortQuest)
                && !manager.IsQuestCompleted(escortQuest)
                && IsLevelMet(escortQuest);

            SetGroundHighlight(isFindTarget || escortOfferable);
        }

        private void SetGroundHighlight(bool value)
        {
            if (groundHighlight != null) groundHighlight.SetActive(value);
        }

        private void HandleQuestStateChanged(QuestData quest)
        {
            if (quest != findQuest && quest != escortQuest) return;
            RefreshGroundHighlight();
        }

        private void HandleObjectiveProgress(QuestData quest, int objectiveIndex)
        {
            if (quest != findQuest && quest != escortQuest) return;
            RefreshGroundHighlight();
        }

        private void HandleLevelUp(int newLevel)
        {
            RefreshGroundHighlight();
        }

        private void BeginEscort()
        {
            if (escortQuest == null) return;

            List<string> waypointIds = null;
            foreach (var objective in escortQuest.Objectives)
            {
                if (objective.type == QuestObjectiveType.EscortNPC)
                {
                    waypointIds = objective.escortWaypointIds;
                    break;
                }
            }

            _route.Clear();
            if (waypointIds != null)
            {
                foreach (var pointId in waypointIds)
                {
                    EscortPoint point = EscortPoint.Find(pointId);
                    if (point == null)
                    {
                        Debug.LogError($"[NPCEmilyController] Không tìm thấy EscortPoint id '{pointId}'", this);
                        continue;
                    }
                    _route.Add(point);
                }
            }

            if (_route.Count == 0) return;

            IsEscorting = true;
            _routeIndex = 0;
            movement?.BeginMoveTo(_route[_routeIndex].Position);
            enemyDirector?.BeginEncounter(transform);
        }

        private void UnlockRouteContainers()
        {
            if (escortQuest == null) return;

            foreach (var objective in escortQuest.Objectives)
            {
                if (objective.type != QuestObjectiveType.EscortNPC) continue;

                foreach (var pointId in objective.escortWaypointIds)
                {
                    EscortPoint point = EscortPoint.Find(pointId);
                    point?.LootContainer?.SetLocked(false);
                }
            }
        }

        private void Update()
        {
            if (!IsEscorting || _waitingLootContainer != null) return;
            if (movement == null || !movement.HasArrived) return;

            EscortPoint arrivedPoint = _route[_routeIndex];

            if (arrivedPoint.LootContainer != null && !arrivedPoint.LootContainer.IsEmpty)
            {
                _waitingLootContainer = arrivedPoint.LootContainer;
                _waitingLootContainer.OnLooted += HandleLootContainerLooted;
                return;
            }

            AdvanceRoute();
        }

        private void HandleLootContainerLooted(LootContainer container)
        {
            if (!container.IsEmpty) return;

            container.OnLooted -= HandleLootContainerLooted;
            _waitingLootContainer = null;

            AdvanceRoute();
        }

        private void AdvanceRoute()
        {
            _routeIndex++;

            if (_routeIndex >= _route.Count)
            {
                IsEscorting = false;
                enemyDirector?.StopEncounter();
                QuestManager.Instance?.NotifyEscortArrived(escortQuest);
                ShowDialogue(escortDoneDialogue);
                return;
            }

            movement?.BeginMoveTo(_route[_routeIndex].Position);
        }

        private void ResetRouteState()
        {
            if (_waitingLootContainer != null)
            {
                _waitingLootContainer.OnLooted -= HandleLootContainerLooted;
                _waitingLootContainer = null;
            }
            _route.Clear();
            _routeIndex = 0;
        }

        private void HandleDamaged(GameObject attacker)
        {
            if (!IsEscorting || attacker == null) return;

            if (_currentThreat == attacker)
            {
                FaceAttacker(attacker.transform);
                return;
            }

            EnterCombat(attacker);
        }

        private void EnterCombat(GameObject attacker)
        {
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

        private void HandleAttackHit()
        {
            if (_currentThreat == null || _currentThreatStats == null) return;
            _currentThreatStats.TakeDamage(attackDamage, gameObject);
        }

        private void HandleThreatDefeated(GameObject source)
        {
            ExitCombat();
        }

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
            ExitCombat();
            movement?.Stop();

            if (!IsEscorting) return;

            IsEscorting = false;
            ResetRouteState();
            enemyDirector?.StopEncounter();
            QuestManager.Instance?.FailQuest(escortQuest);
        }

        private void HandleQuestFailed(QuestData quest)
        {
            if (quest != escortQuest) return;
            IsEscorting = false;
            ExitCombat();
            ResetRouteState();
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