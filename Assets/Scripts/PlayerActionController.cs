using System;
using System.Collections.Generic;
using UnityEngine;
using SimpleSurvival.Actions;
using SimpleSurvival.Targets;
using SimpleSurvival.Items;
using SimpleSurvival.Stats;
using SimpleSurvival.Loot;
using SimpleSurvival.UI;
using SimpleSurvival.UI.Hud;
using SimpleSurvival.AI;

namespace SimpleSurvival.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerActionController : MonoBehaviour
    {
        public static PlayerActionController Instance { get; private set; }

        [SerializeField] private MoveActionConfig moveConfig = new MoveActionConfig();

        [Header("Combat References")]
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private PlayerEquipment playerEquipment;
        [SerializeField] private PlayerInventoryQueries inventoryQueries;
        [SerializeField] private PlayerToolSwapper toolSwapper;
        [SerializeField] private PlayerTargetChecker targetChecker;

        [Header("Pet References")]
        [SerializeField] private Transform dogFollowPoint;

        [Header("Combat Defaults (Unarmed)")]
        [SerializeField] private float unarmedAttackRange = 1.5f;
        [SerializeField] private int unarmedMaxComboIndex = 3;
        [SerializeField] private float comboWindowSeconds = 0.25f;
        [SerializeField] private float unarmedSafetyTimeout = 3f;
        [SerializeField] private float unarmedAttackSpeed = 1.8f;
        [SerializeField] private float unarmedAttackClipLength = 0.5f;

        [Header("Sneak Attack")]
        [SerializeField] private float sneakAttackDamageMultiplier = 2f;

        [Header("Action Ranges")]
        [SerializeField] private float pickupRange = 1f;
        [SerializeField] private float gatherRange = 1f;
        [SerializeField] private float gatherRestartCooldown = 0.15f;
        [SerializeField] private float lootRange = 1.5f;
        [SerializeField] private float npcInteractRange = 1.5f;
        [SerializeField] private float witchEventRange = 1.5f;
        [SerializeField] private float dogHouseInteractRange = 1.5f;
        [SerializeField] private float repairRange = 1.5f;
        [SerializeField] private float followTimeoutSeconds = 5f;

        public IAction CurrentAction { get; private set; }
        public event Action<IAction, IAction> OnActionChanged;

        public CharacterController Controller { get; private set; }
        public Transform PlayerTransform { get; private set; }

        public bool IsAttackHeld { get; private set; }
        public bool AttackInputQueued { get; private set; }

        public PlayerStats PlayerStats => playerStats;
        public PlayerTargetChecker TargetChecker => targetChecker;
        public SimpleSurvival.Input.PlayerInputReader InputReader => _inputReader;
        public Transform DogFollowPoint => dogFollowPoint;

        public void ConsumeAttackQueue()
        {
            AttackInputQueued = false;
        }

        private IdleAction _idleAction;
        private MoveAction _moveAction;
        private SimpleSurvival.Input.PlayerInputReader _inputReader;
        private bool _isDead;
        private float _lastGatherEndTime = -999f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            Controller = GetComponent<CharacterController>();
            PlayerTransform = transform;

            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (playerStats != null)
            {
                playerStats.OnDamagedBy += HandlePlayerDamaged;
                playerStats.OnDeath += HandleDeath;
                playerStats.OnRevived += HandleRevived;
            }
            if (playerEquipment == null) playerEquipment = GetComponentInChildren<PlayerEquipment>();
            if (inventoryQueries == null) inventoryQueries = GetComponentInChildren<PlayerInventoryQueries>();
            if (toolSwapper == null) toolSwapper = GetComponentInChildren<PlayerToolSwapper>();
            if (targetChecker == null) targetChecker = GetComponentInChildren<PlayerTargetChecker>();

            _idleAction = new IdleAction(this);
            _moveAction = new MoveAction(this, moveConfig, playerStats);

            CurrentAction = _idleAction;
            CurrentAction.Init();

            if (playerStats != null)
            {
                playerStats.OnDamagedBy += HandlePlayerDamaged;
                playerStats.OnDeath += HandleDeath;
            }

            _inputReader = GetComponent<SimpleSurvival.Input.PlayerInputReader>();
            if (_inputReader != null)
                _inputReader.OnSneakChanged += HandleSneakChanged;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;

            if (playerStats != null)
            {
                playerStats.OnDamagedBy -= HandlePlayerDamaged;
                playerStats.OnDeath -= HandleDeath;
                playerStats.OnRevived -= HandleRevived;
            }

            if (_inputReader != null)
                _inputReader.OnSneakChanged -= HandleSneakChanged;
        }
        private void HandleRevived()
        {
            _isDead = false;
            SwitchToIdle();
        }

        public void ApplyDeadStateOnLoad()
        {
            _isDead = true;
        }
        private void Update()
        {
            if (_isDead) return;

            CurrentAction.Update(Time.deltaTime);

            if (CurrentAction.IsCompleted)
            {
                HandleActionCompletion(CurrentAction);
                SwitchToIdle();
            }

            if (CurrentAction == _idleAction && IsAttackHeld)
            {
                ITargetable enemy = targetChecker != null ? targetChecker.CurrentEnemy : null;
                RequestAttack(enemy);
            }
        }

        private void HandleActionCompletion(IAction action)
        {
            if (action is AttackAction attack)
            {
                Debug.Log($"[HandleCompletion] AttackAction, WeaponBroke={attack.WeaponBroke}, StackName={attack.WeaponStack?.ItemData.ItemName ?? "null"}");
                if (attack.WeaponBroke)
                {
                    string brokenName = attack.WeaponStack != null ? attack.WeaponStack.ItemData.ItemName : "Weapon";
                    DestroyStackAnywhere(attack.WeaponStack);

                    if (FollowNotifyManager.Instance != null)
                        FollowNotifyManager.Instance.Notify($"{brokenName} broke!", SpeechHudType.Bad);
                }
            }

            if (action is GatherAction)
                _lastGatherEndTime = Time.time;
        }

        public void DestroyStackAnywhere(ItemStack stack)
        {
            Debug.Log($"[DestroyStack] Stack: {stack?.ItemData.ItemName ?? "null"}");
            if (stack == null) return;
            if (DestroyStackFromEquipment(stack))
            {
                Debug.Log("[DestroyStack] Removed from equipment");
                return;
            }
            if (inventoryQueries != null)
            {
                bool removed = inventoryQueries.RemoveItemStack(stack);
                Debug.Log($"[DestroyStack] Removed from inventory: {removed}");
            }
        }

        private bool DestroyStackFromEquipment(ItemStack target)
        {
            if (playerEquipment == null)
            {
                Debug.Log("[DestroyEquip] playerEquipment is null");
                return false;
            }
            var system = playerEquipment.System;
            foreach (var slot in system.Slots)
            {
                for (int i = 0; i < system.SlotCount(slot); i++)
                {
                    ItemStack inSlot = system.GetSlot(slot, i);
                    if (inSlot == target)
                    {
                        Debug.Log($"[DestroyEquip] Match in {slot}[{i}]");
                        system.SetSlotDirect(slot, i, null);
                        return true;
                    }
                }
            }
            Debug.Log($"[DestroyEquip] Target {target.ItemData.ItemName} not found in any equipment slot");
            return false;
        }

        public bool TryRequestAction(IAction newAction)
        {
            if (_isDead) return false;
            if (newAction == null) return false;
            if (!CurrentAction.CanBeInterruptedBy(newAction)) return false;

            SwitchAction(newAction);
            return true;
        }

        public void RequestMove(Vector3 worldDirection, float magnitude, bool sneakHeld)
        {
            if (_isDead) return;

            _moveAction.UpdateInput(worldDirection, magnitude, sneakHeld);

            if (CurrentAction == _moveAction) return;
            if (magnitude < 0.1f) return;

            if (!CurrentAction.CanBeInterruptedBy(_moveAction)) return;
            SwitchAction(_moveAction);
        }

        public bool RequestAttack(ITargetable target)
        {
            if (animator == null) return false;
            if (CurrentAction.Type == ActionType.Attack) return false;

            ItemStack weaponStack = GetEquippedWeaponStack();
            float damage = ResolveAttackDamage(weaponStack);

            bool isSneakAttack = _inputReader != null && _inputReader.IsSneakHeld
                && IsMeleeWeapon(weaponStack)
                && IsTargetUnaware(target);
            if (isSneakAttack)
                damage *= sneakAttackDamageMultiplier;

            float range = ResolveAttackRange(weaponStack);
            int maxComboIndex = ResolveMaxComboIndex(weaponStack);
            float safetyTimeout = ResolveAttackSafetyTimeout(weaponStack);
            float speedMultiplier = ResolveAttackSpeedMultiplier(weaponStack);

            AttackAction attack = new AttackAction(
                this, animator, target, targetChecker, toolSwapper,
                weaponStack,
                damage, range, maxComboIndex, comboWindowSeconds,
                safetyTimeout,
                speedMultiplier);
            return TryRequestAction(attack);
        }

        public void SetAttackHeld(bool held)
        {
            IsAttackHeld = held;
            if (held) AttackInputQueued = true;
        }

        public bool IsGatherHeld { get; private set; }

        public void SetGatherHeld(bool held)
        {
            IsGatherHeld = held;
        }

        public void ForceIdle()
        {
            SwitchToIdle();
        }

        public void CancelSneak()
        {
            var input = GetComponent<SimpleSurvival.Input.PlayerInputReader>();
            if (input != null) input.ForceSneak(false);
        }

        public bool RequestGather(HarvestTarget target)
        {
            if (target == null || !target.CanBeTargeted()) return false;
            if (animator == null || inventoryQueries == null) return false;

            float dist = ComputeDistanceToTarget(target);
            if (dist > gatherRange)
            {
                FollowAction follow = new FollowAction(
                    this, moveConfig, playerStats, target, gatherRange, followTimeoutSeconds,
                    onArrived: () => BeginGatherAction(target));
                return TryRequestAction(follow);
            }

            return BeginGatherAction(target);
        }

        private bool BeginGatherAction(HarvestTarget target)
        {
            if (target == null || !target.CanBeTargeted()) return false;
            if (Time.time - _lastGatherEndTime < gatherRestartCooldown) return false;

            ToolType required = target.RequiredTool;
            GatherToolResolution resolution = ResolveGatherTool(required);

            if (!resolution.HasTool)
            {
                Debug.Log($"[NoTool] Missing tool: {required}");
                if (FollowNotifyManager.Instance != null)
                    FollowNotifyManager.Instance.Notify($"Need {required}", SpeechHudType.Bad);
                return false;
            }

            GatherAction gather = new GatherAction(
                this,
                animator,
                inventoryQueries,
                toolSwapper,
                target,
                resolution.ToolStack,
                resolution.Damage,
                resolution.IsEphemeral);
            return TryRequestAction(gather);
        }

        public bool RequestPickup(PickupTarget target)
        {
            if (CurrentAction is PickupAction) return false;
            if (target == null || !target.CanBeTargeted()) return false;
            if (animator == null || inventoryQueries == null) return false;

            float dist = ComputeDistanceToTarget(target);
            if (dist > pickupRange)
            {
                FollowAction follow = new FollowAction(
                    this, moveConfig, playerStats, target, pickupRange, followTimeoutSeconds,
                    onArrived: () => BeginPickupAction(target));
                return TryRequestAction(follow);
            }

            return BeginPickupAction(target);
        }

        private bool BeginPickupAction(PickupTarget target)
        {
            if (target == null || !target.CanBeTargeted()) return false;

            if (!CanPickupAtLeastOneItem(target))
            {
                Debug.Log("[ActionController] Inventory full, cannot pickup");
                if (FollowNotifyManager.Instance != null)
                    FollowNotifyManager.Instance.Notify("Inventory full!", SpeechHudType.Bad);
                return false;
            }

            PickupAction pickup = new PickupAction(this, animator, inventoryQueries, target);
            return TryRequestAction(pickup);
        }
        public bool RequestLoot(LootContainer target)
        {
            if (target == null || !target.CanBeTargeted()) return false;
            if (animator == null) return false;

            float dist = ComputeDistanceToTarget(target);
            if (dist > lootRange)
            {
                Debug.Log($"[Loot] Too far: {dist:F1}m > {lootRange:F1}m");
                return false;
            }

            if (target.IsUnlocked)
            {
                target.Open();
                if (InventoryPanelController.Instance != null)
                    InventoryPanelController.Instance.OpenLoot(target);
                return true;
            }

            UnlockAction unlock = new UnlockAction(this, animator, target,
                onComplete: () =>
                {
                    if (InventoryPanelController.Instance != null)
                        InventoryPanelController.Instance.OpenLoot(target);
                });
            return TryRequestAction(unlock);
        }

        public bool RequestRepairTower(RepairableTower target)
        {
            if (target == null || !target.CanBeTargeted()) return false;
            if (animator == null || inventoryQueries == null) return false;
            if (target.RequiredItems == null || target.RequiredItems.Count == 0) return false;

            float dist = ComputeDistanceToTarget(target);
            if (dist > repairRange)
            {
                Debug.Log($"[Repair] Too far: {dist:F1}m > {repairRange:F1}m");
                return false;
            }

            RepairRequirement missing = FindMissingRequirement(target.RequiredItems);
            if (missing != null)
            {
                Debug.Log($"[Repair] Missing item: {missing.ItemData.ItemName}");
                if (FollowNotifyManager.Instance != null)
                    FollowNotifyManager.Instance.Notify($"Need {missing.Quantity} {missing.ItemData.ItemName}", SpeechHudType.Bad);
                return false;
            }

            UnlockAction repair = new UnlockAction(this, animator, target,
                onComplete: () => ConsumeRepairItems(target.RequiredItems));
            return TryRequestAction(repair);
        }

        private RepairRequirement FindMissingRequirement(IReadOnlyList<RepairRequirement> requirements)
        {
            foreach (RepairRequirement requirement in requirements)
            {
                if (requirement == null || requirement.ItemData == null) continue;
                if (inventoryQueries.CountItem(requirement.ItemData) < requirement.Quantity)
                    return requirement;
            }
            return null;
        }

        private void ConsumeRepairItems(IReadOnlyList<RepairRequirement> requirements)
        {
            foreach (RepairRequirement requirement in requirements)
            {
                if (requirement == null || requirement.ItemData == null) continue;
                inventoryQueries.RemoveItemAmount(requirement.ItemData, requirement.Quantity);
            }
        }

        public bool RequestNPCInteract(SimpleSurvival.Targets.NPCTargetable target)
        {
            if (target == null || !target.CanBeTargeted()) return false;

            float dist = ComputeDistanceToTarget(target);
            if (dist > npcInteractRange)
            {
                Debug.Log($"[NPC] Too far: {dist:F1}m > {npcInteractRange:F1}m");
                return false;
            }

            var npc = target.GetComponentInParent<SimpleSurvival.AI.BaseNPCController>();
            if (npc == null) return false;

            npc.OnPlayerInteract(gameObject);
            return true;
        }

        public bool RequestDogHouseInteract(SimpleSurvival.Targets.DogHouseTargetable target)
        {
            if (target == null || !target.CanBeTargeted()) return false;

            float dist = ComputeDistanceToTarget(target);
            if (dist > dogHouseInteractRange)
            {
                Debug.Log($"[DogHouse] Too far: {dist:F1}m > {dogHouseInteractRange:F1}m");
                return false;
            }

            var interactable = target.GetComponentInParent<SimpleSurvival.Targets.DogHouseInteractable>();
            if (interactable == null) return false;

            interactable.OnPlayerInteract(gameObject);
            return true;
        }
        public bool RequestWitchEvent(SimpleSurvival.Targets.WitchEventTrap target)
        {
            if (target == null || !target.CanBeTargeted()) return false;
            if (animator == null) return false;

            float dist = ComputeDistanceToTarget(target);
            if (dist > witchEventRange)
            {
                Debug.Log($"[WitchEvent] Too far: {dist:F1}m > {witchEventRange:F1}m");
                return false;
            }

            WitchEventAction action = new WitchEventAction(this, animator, target);
            return TryRequestAction(action);
        }

        private void HandlePlayerDamaged(GameObject attacker)
        {
            if (CurrentAction is UnlockAction unlock)
                unlock.Cancel();
        }

        private void HandleSneakChanged(bool isSneaking)
        {
            if (!isSneaking) return;
            if (CurrentAction.Type != ActionType.Attack) return;

            CurrentAction.Cancel();
            SwitchToIdle();
        }

        private void HandleDeath(GameObject source)
        {
            _isDead = true;
        }

        private bool CanPickupAtLeastOneItem(PickupTarget target)
        {
            foreach (var entry in target.Items)
            {
                if (entry == null || entry.itemData == null || entry.quantity <= 0) continue;
                if (inventoryQueries.CanAddItem(entry.itemData, 1)) return true;
            }
            return false;
        }

        public float ComputeDistanceToTarget(ITargetable target)
        {
            if (target?.Transform == null) return float.MaxValue;

            Vector3 playerPos = PlayerTransform.position;

            if (target.DistanceCollider != null)
            {
                Vector3 closestPoint = target.DistanceCollider.ClosestPoint(playerPos);
                return Vector3.Distance(playerPos, closestPoint);
            }

            float dist = Vector3.Distance(playerPos, target.Transform.position) - target.Radius;
            return dist < 0f ? 0f : dist;
        }

        private ItemStack GetEquippedWeaponStack()
        {
            if (playerEquipment == null) return null;
            var system = playerEquipment.System;

            ItemStack mainWeapon = system.GetSlot(EquipSlot.Weapon, 0);
            if (mainWeapon != null && !mainWeapon.IsBroken)
                return mainWeapon;

            for (int i = 0; i < system.SlotCount(EquipSlot.QuickSlot); i++)
            {
                ItemStack quickStack = system.GetSlot(EquipSlot.QuickSlot, i);
                if (quickStack == null) continue;
                if (!quickStack.ItemData.HasAbility<WeaponAbility>()) continue;
                if (quickStack.IsBroken) continue;

                system.SetSlotDirect(EquipSlot.Weapon, 0, quickStack);
                system.SetSlotDirect(EquipSlot.QuickSlot, i, mainWeapon);
                return quickStack;
            }

            return mainWeapon;
        }

        private float ResolveAttackDamage(ItemStack weaponStack)
        {
            WeaponAbility weapon = GetWeaponAbility(weaponStack);
            float damage = weapon != null ? weapon.Damage : (playerStats != null ? playerStats.BaseDamage : 0f);

            if (playerStats != null)
                damage *= playerStats.GetDamageMultiplier(weapon?.Category);

            return damage;
        }

        private float ResolveAttackRange(ItemStack weaponStack)
        {
            WeaponAbility weapon = GetWeaponAbility(weaponStack);
            float range = weapon != null ? weapon.Range : unarmedAttackRange;

            if (playerStats != null)
                range *= playerStats.GetRangeMultiplier(weapon?.Category);

            return range;
        }

        private int ResolveMaxComboIndex(ItemStack weaponStack)
        {
            WeaponAbility weapon = GetWeaponAbility(weaponStack);
            if (weapon != null) return weapon.MaxComboIndex;
            return unarmedMaxComboIndex;
        }

        private float ResolveAttackSafetyTimeout(ItemStack weaponStack)
        {
            WeaponAbility weapon = GetWeaponAbility(weaponStack);
            if (weapon != null) return weapon.SafetyTimeout;
            return unarmedSafetyTimeout;
        }

        private float ResolveAttackSpeedMultiplier(ItemStack weaponStack)
        {
            WeaponAbility weapon = GetWeaponAbility(weaponStack);
            float speed = weapon != null
                ? weapon.AttackSpeed * weapon.AttackClipLength
                : unarmedAttackSpeed * unarmedAttackClipLength;

            if (playerStats != null)
                speed *= playerStats.GetAttackSpeedMultiplier(weapon?.Category);

            return speed;
        }

        private WeaponAbility GetWeaponAbility(ItemStack stack)
        {
            if (stack == null || stack.IsBroken) return null;
            return stack.ItemData.GetAbility<WeaponAbility>();
        }

        private bool IsTargetUnaware(ITargetable target)
        {
            if (target == null) return true;

            MonoBehaviour targetMb = target as MonoBehaviour;
            if (targetMb == null) return true;

            BaseEnemyController enemy = targetMb.GetComponentInParent<BaseEnemyController>();
            if (enemy == null) return true;

            return !enemy.HasDetectedPlayer;
        }

        private bool IsMeleeWeapon(ItemStack weaponStack)
        {
            WeaponAbility weapon = GetWeaponAbility(weaponStack);
            if (weapon == null) return true;

            switch (weapon.Category)
            {
                case WeaponCategory.Fists:
                case WeaponCategory.Melee1H:
                case WeaponCategory.Melee2H:
                    return true;
                default:
                    return false;
            }
        }

        private struct GatherToolResolution
        {
            public bool HasTool;
            public ItemStack ToolStack;
            public float Damage;
            public bool IsEphemeral;
        }

        private GatherToolResolution ResolveGatherTool(ToolType required)
        {
            GatherToolResolution result = new GatherToolResolution();

            if (playerEquipment != null)
            {
                var system = playerEquipment.System;

                ItemStack equipped = system.GetSlot(EquipSlot.Weapon, 0);
                if (equipped != null && !equipped.IsBroken)
                {
                    ToolAbility equippedTool = equipped.ItemData.GetAbility<ToolAbility>();
                    if (equippedTool != null && equippedTool.ToolType == required)
                    {
                        result.HasTool = true;
                        result.ToolStack = equipped;
                        result.Damage = equippedTool.Damage;
                        result.IsEphemeral = false;
                        return result;
                    }
                }

                for (int i = 0; i < system.SlotCount(EquipSlot.QuickSlot); i++)
                {
                    ItemStack quickStack = system.GetSlot(EquipSlot.QuickSlot, i);
                    if (quickStack == null || quickStack.IsBroken) continue;

                    ToolAbility quickTool = quickStack.ItemData.GetAbility<ToolAbility>();
                    if (quickTool == null || quickTool.ToolType != required) continue;

                    result.HasTool = true;
                    result.ToolStack = quickStack;
                    result.Damage = quickTool.Damage;
                    result.IsEphemeral = true;
                    return result;
                }
            }

            if (inventoryQueries != null)
            {
                ItemStack stack = inventoryQueries.FindToolItemLowestDurability(required);
                if (stack != null)
                {
                    ToolAbility tool = stack.ItemData.GetAbility<ToolAbility>();
                    if (tool != null)
                    {
                        result.HasTool = true;
                        result.ToolStack = stack;
                        result.Damage = tool.Damage;
                        result.IsEphemeral = true;
                    }
                }
            }

            return result;
        }

        private void SwitchAction(IAction newAction)
        {
            IAction oldAction = CurrentAction;

            if (oldAction != _idleAction && oldAction != _moveAction)
                oldAction.Cancel();
            else if (oldAction == _moveAction && newAction != _moveAction)
                oldAction.Cancel();

            CurrentAction = newAction;
            newAction.Init();

            OnActionChanged?.Invoke(oldAction, newAction);
        }

        private void SwitchToIdle()
        {
            if (CurrentAction == _idleAction) return;

            IAction oldAction = CurrentAction;

            if (oldAction == _moveAction)
                oldAction.Cancel();

            CurrentAction = _idleAction;
            _idleAction.Init();

            OnActionChanged?.Invoke(oldAction, _idleAction);
        }
    }
}