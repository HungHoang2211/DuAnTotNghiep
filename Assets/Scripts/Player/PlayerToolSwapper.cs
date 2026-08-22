using System;
using UnityEngine;
using SimpleSurvival.Items;

namespace SimpleSurvival.Player
{
    public class PlayerToolSwapper : MonoBehaviour
    {
        private static readonly int ParamGatherLingerEnd = Animator.StringToHash("GatherLingerEnd");

        [SerializeField] private Animator animator;
        [SerializeField] private PlayerAnimator playerAnimator;
        [SerializeField] private float lingerDuration = 0.3f;
        [SerializeField] private float maxTransitionWait = 2f;
        [Range(0f, 1f)]
        [SerializeField] private float transitionSwapThreshold = 0.7f;

        private enum PendingActionType { None, RevertToWeapon, SwitchToTool, WaitingForRevertTransition }

        private PendingActionType _pendingAction = PendingActionType.None;
        private ToolAbility _pendingTool;
        private float _pendingTimer;
        private bool _skipTransitionCheckThisFrame;

        public bool IsSwapped { get; private set; }
        public ToolAbility CurrentTool { get; private set; }

        public event Action OnToolVisualStateChanged;

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (playerAnimator == null) playerAnimator = GetComponent<PlayerAnimator>();
        }

        private void Update()
        {
            switch (_pendingAction)
            {
                case PendingActionType.None:
                    return;

                case PendingActionType.RevertToWeapon:
                    _pendingTimer -= Time.deltaTime;
                    if (_pendingTimer > 0f) return;
                    BeginRevertTransition();
                    return;

                case PendingActionType.SwitchToTool:
                    _pendingTimer -= Time.deltaTime;
                    if (_pendingTimer > 0f) return;

                    ToolAbility tool = _pendingTool;
                    _pendingAction = PendingActionType.None;
                    _pendingTool = null;
                    ApplySwapIn(tool);
                    return;

                case PendingActionType.WaitingForRevertTransition:
                    if (_skipTransitionCheckThisFrame)
                    {
                        _skipTransitionCheckThisFrame = false;
                        return;
                    }

                    _pendingTimer -= Time.deltaTime;

                    bool reachedThreshold = HasReachedSwapThreshold();

                    if (!reachedThreshold && _pendingTimer > 0f) return;

                    if (_pendingTimer <= 0f && !reachedThreshold)
                        Debug.LogWarning("[PlayerToolSwapper] Timeout chờ transition GatherLingerEnd, ép hoàn tất revert.");

                    FinishRevert();
                    return;
            }
        }

        private bool HasReachedSwapThreshold()
        {
            if (animator == null) return true;
            if (!animator.IsInTransition(0)) return true;

            AnimatorTransitionInfo info = animator.GetAnimatorTransitionInfo(0);
            return info.normalizedTime >= transitionSwapThreshold;
        }

        public bool SwapIn(ToolAbility tool)
        {
            if (tool == null || tool.OverrideController == null) return false;
            if (animator == null) return false;

            _pendingAction = PendingActionType.None;
            _pendingTool = null;
            ApplySwapIn(tool);
            return true;
        }

        public void RequestRevert()
        {
            if (!IsSwapped) return;

            _pendingAction = PendingActionType.RevertToWeapon;
            _pendingTool = null;
            _pendingTimer = lingerDuration;
        }

        public void RequestSwitchTool(ToolAbility newTool)
        {
            if (newTool == null || newTool.OverrideController == null) return;

            _pendingAction = PendingActionType.SwitchToTool;
            _pendingTool = newTool;
            _pendingTimer = 0f;
        }

        public void ForceRevertNow()
        {
            _pendingAction = PendingActionType.None;
            _pendingTool = null;
            ApplyRevertToWeaponImmediate();
        }

        private void BeginRevertTransition()
        {
            if (animator != null && playerAnimator != null)
            {
                AnimatorOverrideController weaponController = playerAnimator.ResolveCurrentWeaponController();
                if (weaponController != null)
                    animator.runtimeAnimatorController = weaponController;
            }

            if (animator != null)
                animator.SetTrigger(ParamGatherLingerEnd);

            _pendingAction = PendingActionType.WaitingForRevertTransition;
            _pendingTimer = maxTransitionWait;
            _skipTransitionCheckThisFrame = true;
        }

        private void FinishRevert()
        {
            _pendingAction = PendingActionType.None;

            CurrentTool = null;
            IsSwapped = false;
            OnToolVisualStateChanged?.Invoke();
        }

        private void ApplySwapIn(ToolAbility tool)
        {
            if (tool == null || tool.OverrideController == null || animator == null) return;

            animator.runtimeAnimatorController = tool.OverrideController;
            CurrentTool = tool;
            IsSwapped = true;
            OnToolVisualStateChanged?.Invoke();
        }

        private void ApplyRevertToWeaponImmediate()
        {
            if (!IsSwapped) return;

            if (animator != null && playerAnimator != null)
            {
                AnimatorOverrideController weaponController = playerAnimator.ResolveCurrentWeaponController();
                if (weaponController != null)
                    animator.runtimeAnimatorController = weaponController;
            }

            CurrentTool = null;
            IsSwapped = false;
            OnToolVisualStateChanged?.Invoke();
        }
    }
}