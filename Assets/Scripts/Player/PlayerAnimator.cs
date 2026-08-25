using System.Collections.Generic;
using UnityEngine;
using SimpleSurvival.Actions;
using SimpleSurvival.Input;
using SimpleSurvival.Items;

namespace SimpleSurvival.Player
{
    [RequireComponent(typeof(PlayerActionController))]
    public class PlayerAnimator : MonoBehaviour
    {
        private static readonly int ParamMoveSpeed = Animator.StringToHash("MoveSpeed");
        private static readonly int ParamMoveMode = Animator.StringToHash("MoveMode");

        private static readonly string[] GatherClipNames = { "action_gather_hatchet", "action_gather_idle" };

        [SerializeField] private Animator animator;
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private PlayerEquipment playerEquipment;
        [SerializeField] private PlayerToolSwapper toolSwapper;
        [SerializeField] private int moveModeNormal = 0;
        [SerializeField] private int moveModeSneak = 1;
        [SerializeField] private float speedDampTime = 0.1f;

        [Header("Animation")]
        [Tooltip("AnimatorController gốc (PlayerBase.controller). Nếu để trống, tự lấy từ Default Override Controller.")]
        [SerializeField] private RuntimeAnimatorController baseController;
        [Tooltip("Override Controller khi không equip weapon (tay không). Drag Fists.overrideController vào đây.")]
        [SerializeField] private AnimatorOverrideController defaultOverrideController;

        private PlayerActionController _actionController;
        private bool _weaponSlotDirty;
        private ItemStack _pendingWeaponSlotStack;
        private bool _hasPendingWeaponSlotStack;

        private AnimatorOverrideController _runtimeController;
        private readonly List<KeyValuePair<AnimationClip, AnimationClip>> _overridesBuffer = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        private readonly List<KeyValuePair<AnimationClip, AnimationClip>> _applyBuffer = new List<KeyValuePair<AnimationClip, AnimationClip>>();

        private void Awake()
        {
            _actionController = GetComponent<PlayerActionController>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (inputReader == null) inputReader = GetComponent<PlayerInputReader>();
            if (playerEquipment == null) playerEquipment = GetComponentInChildren<PlayerEquipment>();
            if (toolSwapper == null) toolSwapper = GetComponent<PlayerToolSwapper>();

            if (baseController == null && defaultOverrideController != null)
                baseController = defaultOverrideController.runtimeAnimatorController;
        }

        private void Start()
        {
            if (animator != null && baseController != null)
            {
                _runtimeController = new AnimatorOverrideController(baseController);
                _runtimeController.name = "PlayerRuntimeOverride";
                animator.runtimeAnimatorController = _runtimeController;
            }

            if (playerEquipment != null && playerEquipment.System != null)
                playerEquipment.System.OnSlotChanged += HandleSlotChanged;

            ApplyWeaponSlot(ResolveCurrentWeaponStack());
        }

        private void OnDestroy()
        {
            if (playerEquipment != null && playerEquipment.System != null)
                playerEquipment.System.OnSlotChanged -= HandleSlotChanged;
        }

        private void Update()
        {
            if (animator == null) return;

            float moveSpeed = 0f;
            if (_actionController.CurrentAction is IMovingAction moving)
                moveSpeed = moving.NormalizedSpeed;

            bool isSneaking = inputReader != null && inputReader.IsSneakHeld;

            animator.SetFloat(ParamMoveSpeed, moveSpeed, speedDampTime, Time.deltaTime);
            animator.SetInteger(ParamMoveMode, isSneaking ? moveModeSneak : moveModeNormal);
        }

        private void LateUpdate()
        {
            if (!_weaponSlotDirty) return;
            _weaponSlotDirty = false;

            if (toolSwapper != null && toolSwapper.IsSwapped) return;

            ItemStack stack = _hasPendingWeaponSlotStack
                ? _pendingWeaponSlotStack
                : ResolveCurrentWeaponStack();

            ApplyWeaponSlot(stack);
        }

        private void HandleSlotChanged(EquipSlot slot, int index, ItemStack stack)
        {
            if (slot != EquipSlot.Weapon) return;

            _weaponSlotDirty = true;
            _pendingWeaponSlotStack = stack;
            _hasPendingWeaponSlotStack = true;
        }

        private ItemStack ResolveCurrentWeaponStack()
        {
            return playerEquipment != null ? playerEquipment.System.GetSlot(EquipSlot.Weapon, 0) : null;
        }

        private void ApplyWeaponSlot(ItemStack stack)
        {
            AnimatorOverrideController source = ResolveOverrideController(stack);
            bool isToolInWeaponSlot = HasToolAbility(stack);

            ApplyOverrideGroup(source, includeGatherGroup: isToolInWeaponSlot);
        }

        public void ApplyGatherOverrides(AnimatorOverrideController toolOverrideController)
        {
            ApplyOverrideGroup(toolOverrideController, includeGatherGroup: true, gatherOnly: true);
        }

        public void RestoreGatherOverridesForCurrentWeapon()
        {
            ItemStack stack = ResolveCurrentWeaponStack();
            if (!HasToolAbility(stack)) return;

            AnimatorOverrideController source = ResolveOverrideController(stack);
            ApplyOverrideGroup(source, includeGatherGroup: true, gatherOnly: true);
        }

        private void ApplyOverrideGroup(AnimatorOverrideController source, bool includeGatherGroup, bool gatherOnly = false)
        {
            if (source == null || _runtimeController == null) return;

            _overridesBuffer.Clear();
            source.GetOverrides(_overridesBuffer);

            _applyBuffer.Clear();
            foreach (var pair in _overridesBuffer)
            {
                if (pair.Key == null) continue;

                bool isGatherClip = IsGatherClipName(pair.Key.name);

                if (gatherOnly && !isGatherClip) continue;
                if (isGatherClip && !includeGatherGroup) continue;

                _applyBuffer.Add(pair);
            }

            _runtimeController.ApplyOverrides(_applyBuffer);
        }

        private static bool IsGatherClipName(string clipName)
        {
            foreach (string name in GatherClipNames)
            {
                if (clipName == name) return true;
            }
            return false;
        }

        private static bool HasToolAbility(ItemStack stack)
        {
            return stack != null && stack.ItemData.GetAbility<ToolAbility>() != null;
        }

        private AnimatorOverrideController ResolveOverrideController(ItemStack stack)
        {
            if (stack == null) return defaultOverrideController;

            WeaponAbility weapon = stack.ItemData.GetAbility<WeaponAbility>();
            if (weapon != null && weapon.OverrideController != null)
                return weapon.OverrideController;

            ToolAbility tool = stack.ItemData.GetAbility<ToolAbility>();
            if (tool != null && tool.OverrideController != null)
                return tool.OverrideController;

            return defaultOverrideController;
        }
    }
}