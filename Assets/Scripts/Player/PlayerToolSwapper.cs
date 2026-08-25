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

        private enum PendingActionType { None, RevertToWeapon, SwitchToTool }

        private PendingActionType _pendingAction = PendingActionType.None;
        private ToolAbility _pendingTool;
        private float _pendingTimer;

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
            if (_pendingAction == PendingActionType.None) return;

            _pendingTimer -= Time.deltaTime;
            if (_pendingTimer > 0f) return;

            PendingActionType action = _pendingAction;
            ToolAbility tool = _pendingTool;

            _pendingAction = PendingActionType.None;
            _pendingTool = null;

            if (action == PendingActionType.RevertToWeapon)
                ApplyRevertToWeapon(triggerLingerEnd: true);
            else if (action == PendingActionType.SwitchToTool)
                ApplySwapIn(tool);
        }

        public bool SwapIn(ToolAbility tool)
        {
            if (tool == null || tool.OverrideController == null) return false;

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
            ApplyRevertToWeapon(triggerLingerEnd: false);
        }

        private void ApplySwapIn(ToolAbility tool)
        {
            if (tool == null || tool.OverrideController == null) return;

            playerAnimator?.ApplyGatherOverrides(tool.OverrideController);

            CurrentTool = tool;
            IsSwapped = true;
            OnToolVisualStateChanged?.Invoke();
        }

        private void ApplyRevertToWeapon(bool triggerLingerEnd)
        {
            if (!IsSwapped) return;

            playerAnimator?.RestoreGatherOverridesForCurrentWeapon();

            if (triggerLingerEnd && animator != null)
                animator.SetTrigger(ParamGatherLingerEnd);

            CurrentTool = null;
            IsSwapped = false;
            OnToolVisualStateChanged?.Invoke();
        }
    }
}