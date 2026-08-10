using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleSurvival.Stats;
using SimpleSurvival.UI.Hud;
using SimpleSurvival.Quests;
using SimpleSurvival.Loot;
using SimpleSurvival.Player;
using SimpleSurvival.Progression;

namespace SimpleSurvival.AI
{
    public sealed class NPCEmilyController : BaseNPCController
    {
        [Header("Định danh")]
        [SerializeField] private string npcId = "emily";

        [Header("Quest MrBeat")]
        [SerializeField] private QuestData findQuest;
        [SerializeField] private string findQuestInventoryFullDialogue = "Your bag is full, please make some space before I can reward you.";

        [Header("Quest Emily")]
        [SerializeField] private QuestData escortQuest;

        [Header("Quest Emily - Defeat ZombieWitch")]
        [SerializeField] private QuestData killWitchQuest;
        [SerializeField] private string killWitchInProgressDialogue = "That witch is still out there somewhere, be careful.";
        [SerializeField] private string killWitchInventoryFullDialogue = "Your bag is full, please make some space before I can reward you.";

        [Header("Quest Emily - Watch Tower")]
        [SerializeField] private QuestData repairTowerQuest;
        [SerializeField] private string repairTowerInProgressDialogue = "The broadcast tower still needs fixing.";

        [Header("Dialogue")]
        [SerializeField] private string escortOfferDialogue = "Could you escort me there?";
        [SerializeField] private string escortInProgressDialogue = "Let's go, I'll follow you.";
        [SerializeField] private string escortDoneDialogue = "Thank you so much!";
        [SerializeField] private string escortInventoryFullDialogue = "Thank you for getting me here, but your bag is full. Please make some space so I can reward you.";
        [SerializeField] private string allDoneDialogue = "I'm fine now, thank you.";
        [SerializeField] private string lockedByLevelDialogue = "You need to reach a higher level to accept this quest.";

        [Header("Phản đòn")]
        [SerializeField] private float attackInterval = 1.2f;

        [SerializeField] private float attackDamage = 10f;

        [Header("Hồi sinh sau khi chết lúc Escort")]
        [SerializeField] private float respawnDelay = 5f;

        [Header("Refs")]
        [SerializeField] private NPCEmilyMovement movement;
        [SerializeField] private NPCEmilyStats stats;
        [SerializeField] private NPCEmilyAnimator animatorController;
        [SerializeField] private NPCEmilyRagdollController ragdollController;
        [SerializeField] private EscortEnemyDirector enemyDirector;
        [SerializeField] private GameObject groundHighlight;

        [Header("Debug / Testing")]
        [Tooltip("CHỈ DÙNG ĐỂ TEST - nhớ tắt trước khi build thật. Khi bật: lúc Start sẽ tự ép " +
                 "hoàn thành Find Quest, Escort Quest và Kill Witch Quest (nếu đã gán và chưa hoàn thành) " +
                 "để có thể nhận thẳng Repair Tower Quest mà không cần chơi tuần tự qua các bước trước đó.")]
        [SerializeField] private bool debugSkipToRepairTower = false;

        public bool IsEscorting { get; private set; }

        private readonly List<EscortPoint> _route = new List<EscortPoint>();
        private int _routeIndex;
        private LootContainer _waitingLootContainer;

        private GameObject _currentThreat;
        private BaseStats _currentThreatStats;
        private Coroutine _combatRoutine;
        private Coroutine _respawnRoutine;
        private bool _blockInteractUntilReturn;
        private bool _respawnPositionReady;

        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;

        protected override void Awake()
        {
            base.Awake();
            _spawnPosition = transform.position;
            _spawnRotation = transform.rotation;
        }

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

                if (debugSkipToRepairTower)
                {
                    if (findQuest != null && !manager.IsQuestCompleted(findQuest))
                        manager.DebugForceCompleteQuest(findQuest);

                    if (escortQuest != null && !manager.IsQuestCompleted(escortQuest))
                        manager.DebugForceCompleteQuest(escortQuest);

                    if (killWitchQuest != null && !manager.IsQuestCompleted(killWitchQuest))
                        manager.DebugForceCompleteQuest(killWitchQuest);
                }

                if (escortQuest != null && manager.IsQuestCompleted(escortQuest))
                {
                    EscortPoint finalPoint = GetFinalEscortPoint();
                    if (finalPoint != null)
                        movement?.WarpTo(finalPoint.Position);
                }
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

            if (_respawnRoutine != null)
            {
                StopCoroutine(_respawnRoutine);
                _respawnRoutine = null;
            }
        }

        public override void OnPlayerInteract(GameObject player)
        {
            var manager = QuestManager.Instance;
            if (manager == null) return;

            if (_blockInteractUntilReturn) return;

            if (findQuest != null && manager.IsQuestActive(findQuest))
            {
                manager.NotifyNPCFound(npcId);
                if (manager.IsReadyToTurnIn(findQuest))
                {
                    if (manager.HasSpaceForRewards(findQuest))
                    {
                        manager.CompleteQuest(findQuest);
                        ShowDialogue(findQuest.TurnInDialogue);
                    }
                    else
                    {
                        ShowDialogue(findQuestInventoryFullDialogue);
                    }
                }
                return;
            }

            if (escortQuest != null && manager.IsQuestActive(escortQuest) && !IsEscorting && manager.IsReadyToTurnIn(escortQuest))
            {
                if (manager.HasSpaceForRewards(escortQuest))
                {
                    manager.CompleteQuest(escortQuest);
                    ShowDialogue(escortDoneDialogue);
                }
                else
                {
                    ShowDialogue(escortInventoryFullDialogue);
                }
                return;
            }

            if (escortQuest != null && manager.IsQuestActive(escortQuest) && !IsEscorting)
            {
                ShowDialogue(escortInProgressDialogue);
                BeginEscort();
                return;
            }

            bool findRequirementMet = findQuest == null || manager.IsQuestCompleted(findQuest);

            bool escortNotOfferedYet = escortQuest != null
                && findRequirementMet
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

            if (killWitchQuest != null && manager.IsQuestActive(killWitchQuest))
            {
                if (manager.IsReadyToTurnIn(killWitchQuest))
                {
                    if (manager.HasSpaceForRewards(killWitchQuest))
                    {
                        ShowDialogue(killWitchQuest.TurnInDialogue);
                        manager.CompleteQuest(killWitchQuest);
                        RefreshGroundHighlight();
                    }
                    else
                    {
                        ShowDialogue(killWitchInventoryFullDialogue);
                    }
                }
                else
                {
                    ShowDialogue(killWitchInProgressDialogue);
                }
                return;
            }

            bool killWitchNotOfferedYet = killWitchQuest != null
                && escortQuest != null
                && manager.IsQuestCompleted(escortQuest)
                && !manager.IsQuestActive(killWitchQuest)
                && !manager.IsQuestCompleted(killWitchQuest);

            if (killWitchNotOfferedYet)
            {
                if (!IsLevelMet(killWitchQuest))
                {
                    ShowDialogue(lockedByLevelDialogue);
                    return;
                }

                ShowDialogue(killWitchQuest.OfferDialogue);
                manager.StartQuest(killWitchQuest);
                RefreshGroundHighlight();
                return;
            }

            if (repairTowerQuest != null && manager.IsQuestActive(repairTowerQuest))
            {
                ShowDialogue(repairTowerInProgressDialogue);
                return;
            }

            bool repairTowerNotOfferedYet = repairTowerQuest != null
                && killWitchQuest != null
                && manager.IsQuestCompleted(killWitchQuest)
                && !manager.IsQuestActive(repairTowerQuest)
                && !manager.IsQuestCompleted(repairTowerQuest);

            if (repairTowerNotOfferedYet)
            {
                if (!IsLevelMet(repairTowerQuest))
                {
                    ShowDialogue(lockedByLevelDialogue);
                    return;
                }

                ShowDialogue(repairTowerQuest.OfferDialogue);
                manager.StartQuest(repairTowerQuest);
                RefreshGroundHighlight();
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

            bool findRequirementMet = findQuest == null || manager.IsQuestCompleted(findQuest);

            bool escortOfferable = escortQuest != null
                && findRequirementMet
                && !manager.IsQuestActive(escortQuest)
                && !manager.IsQuestCompleted(escortQuest)
                && IsLevelMet(escortQuest);

            bool killWitchOfferable = killWitchQuest != null
                && escortQuest != null
                && manager.IsQuestCompleted(escortQuest)
                && !manager.IsQuestActive(killWitchQuest)
                && !manager.IsQuestCompleted(killWitchQuest)
                && IsLevelMet(killWitchQuest);

            bool killWitchReadyToTurnIn = killWitchQuest != null
                && manager.IsQuestActive(killWitchQuest)
                && manager.IsReadyToTurnIn(killWitchQuest);

            bool repairTowerOfferable = repairTowerQuest != null
                && killWitchQuest != null
                && manager.IsQuestCompleted(killWitchQuest)
                && !manager.IsQuestActive(repairTowerQuest)
                && !manager.IsQuestCompleted(repairTowerQuest)
                && IsLevelMet(repairTowerQuest);

            SetGroundHighlight(isFindTarget || escortOfferable || killWitchOfferable || killWitchReadyToTurnIn || repairTowerOfferable);
        }

        private void SetGroundHighlight(bool value)
        {
            if (groundHighlight != null) groundHighlight.SetActive(value);
        }

        private void HandleQuestStateChanged(QuestData quest)
        {
            if (quest != findQuest && quest != escortQuest && quest != killWitchQuest && quest != repairTowerQuest) return;
            RefreshGroundHighlight();
        }

        private void HandleObjectiveProgress(QuestData quest, int objectiveIndex)
        {
            if (quest != findQuest && quest != escortQuest && quest != killWitchQuest && quest != repairTowerQuest) return;
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
        private EscortPoint GetFinalEscortPoint()
        {
            foreach (var objective in escortQuest.Objectives)
            {
                if (objective.type != QuestObjectiveType.EscortNPC) continue;
                if (objective.escortWaypointIds == null || objective.escortWaypointIds.Count == 0) return null;

                string lastPointId = objective.escortWaypointIds[objective.escortWaypointIds.Count - 1];
                return EscortPoint.Find(lastPointId);
            }
            return null;
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
            if (_blockInteractUntilReturn && _respawnPositionReady)
                TryClearRecoveryBlock();

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

        private void TryClearRecoveryBlock()
        {
            Transform player = PlayerActionController.Instance != null ? PlayerActionController.Instance.PlayerTransform : null;
            if (player == null) return;

            if (Vector3.Distance(player.position, _spawnPosition) > InteractRange) return;

            _blockInteractUntilReturn = false;
            _respawnPositionReady = false;
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

                QuestManager manager = QuestManager.Instance;
                manager?.NotifyEscortArrived(escortQuest);

                if (manager != null && manager.IsQuestCompleted(escortQuest))
                    ShowDialogue(escortDoneDialogue);
                else
                    ShowDialogue(escortInventoryFullDialogue);

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

            _blockInteractUntilReturn = true;
            _respawnPositionReady = false;

            if (_respawnRoutine != null) StopCoroutine(_respawnRoutine);
            _respawnRoutine = StartCoroutine(RespawnAfterDelay());
        }

        private IEnumerator RespawnAfterDelay()
        {
            yield return new WaitForSeconds(respawnDelay);

            ragdollController?.ResetRagdoll();
            movement?.WarpTo(_spawnPosition);
            transform.rotation = _spawnRotation;
            stats?.RestoreHP(stats.MaxHP);

            RefreshGroundHighlight();
            _respawnPositionReady = true;
            _respawnRoutine = null;
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