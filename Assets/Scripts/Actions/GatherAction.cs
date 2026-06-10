using System;
using UnityEngine;
using SimpleSurvival.Items;
using SimpleSurvival.Player;
using SimpleSurvival.Stats;
using SimpleSurvival.Targets;

namespace SimpleSurvival.Actions
{
    public class GatherAction : IAction
    {
        private static readonly int ParamGather = Animator.StringToHash("Gather");

        public ActionType Type => ActionType.Gather;
        public bool IsCompleted { get; private set; }
        public event Action<IAction> Completed;

        private readonly PlayerActionController _controller;
        private readonly Animator _animator;
        private readonly PlayerInventoryQueries _inventoryQueries;
        private readonly PlayerToolSwapper _toolSwapper;
        private readonly PlayerAnimator _playerAnimator;
        private readonly HarvestTarget _target;
        private readonly float _damage;
        private readonly bool _isEphemeral;

        private ItemStack _toolStack;
        private bool _hitAppliedThisChop;
        private bool _targetDepleted;

        public GatherAction(
            PlayerActionController controller,
            Animator animator,
            PlayerInventoryQueries inventoryQueries,
            PlayerToolSwapper toolSwapper,
            PlayerAnimator playerAnimator,
            HarvestTarget target,
            ItemStack toolStack,
            float damage,
            bool isEphemeral)
        {
            _controller = controller;
            _animator = animator;
            _inventoryQueries = inventoryQueries;
            _toolSwapper = toolSwapper;
            _playerAnimator = playerAnimator;
            _target = target;
            _toolStack = toolStack;
            _damage = damage;
            _isEphemeral = isEphemeral;
        }

        public bool CanBeInterruptedBy(IAction newAction)
        {
            if (newAction.Type == ActionType.Move) return false;
            if (newAction.Type == ActionType.Gather) return false;
            return true;
        }


        public void Init()
        {
            _controller.CancelSneak();
            _target.Stats.OnDepleted += HandleTargetDepleted;

            if (_isEphemeral && _toolSwapper != null && _toolStack != null)
            {
                ToolAbility tool = _toolStack.ItemData.GetAbility<ToolAbility>();
                _toolSwapper.SwapIn(tool);
            }

            StartChop();
        }

        public void Update(float deltaTime) { }

        public void Cancel()
        {
            Debug.Log($"[GatherCancel] at {Time.time:F2}, toolDur={_toolStack?.CurrentDurability}");
            CompleteAction();
        }

        public void HandleHit()
        {
            Debug.Log($"[GatherHit] Fire at {Time.time:F2}, hitAlreadyApplied={_hitAppliedThisChop}, toolDur={_toolStack?.CurrentDurability}, targetHP={_target?.Stats?.HP}");
            if (_hitAppliedThisChop) return;
            _hitAppliedThisChop = true;

            if (_target == null || _target.Stats == null) return;
            if (_target.Stats.IsDepleted) return;

            FacingTarget();
            _target.Stats.TakeDamage(_damage);

            Debug.Log($"[GatherHit] After TakeDamage, targetHP={_target.Stats.HP}");
            ConsumeToolDurability();
            Debug.Log($"[GatherHit] After ConsumeDur, toolDur={_toolStack?.CurrentDurability}");
        }

        public void HandleEnd()
        {
            if (_targetDepleted)
            {
                DropItems();
                CompleteAction();
                return;
            }

            if (_toolStack == null || _toolStack.IsBroken)
            {
                if (!TrySwapToReplacementTool())
                {
                    Debug.Log("[ToolBroken] No replacement tool available");
                    CompleteAction();
                    return;
                }
            }

            if (_controller.IsGatherHeld)
            {
                StartChop();
                return;
            }

            CompleteAction();
        }

        private void ConsumeToolDurability()
        {
            if (_toolStack == null) return;
            if (!_toolStack.ItemData.IsDurable) return;

            bool broke = _toolStack.ReduceDurability();
            if (broke)
            {
                Debug.Log($"[ToolBroken] {_toolStack.ItemData.ItemName} broke");

                if (_isEphemeral && _inventoryQueries != null)
                    _inventoryQueries.RemoveItemStack(_toolStack);

                _toolStack = null;
            }
        }

        private bool TrySwapToReplacementTool()
        {
            if (!_isEphemeral) return false;
            if (_inventoryQueries == null || _target == null) return false;

            ItemStack replacement = _inventoryQueries.FindToolItemLowestDurability(_target.RequiredTool);
            if (replacement == null) return false;

            ToolAbility tool = replacement.ItemData.GetAbility<ToolAbility>();
            if (tool == null) return false;

            if (_toolSwapper != null)
            {
                _toolSwapper.SwapOut();
                _toolSwapper.SwapIn(tool);
            }

            _toolStack = replacement;
            return true;
        }

        private void DropItems()
        {
            if (_inventoryQueries == null || _target == null || _target.ItemData == null) return;

            int qty = _target.RollQuantity();
            if (qty > 0)
                _inventoryQueries.AddItem(_target.ItemData, qty);
        }

        private void HandleTargetDepleted()
        {
            _targetDepleted = true;
        }

        private void StartChop()
        {
            Debug.Log($"[GatherStart] StartChop at {Time.time:F2}, prevHitApplied={_hitAppliedThisChop}, toolDur={_toolStack?.CurrentDurability}");
            FacingTarget();
            _hitAppliedThisChop = false;
            _animator.SetTrigger(ParamGather);
        }

        private void CompleteAction()
        {
            if (IsCompleted) return;

            if (_target != null && _target.Stats != null)
                _target.Stats.OnDepleted -= HandleTargetDepleted;

            if (_isEphemeral && _toolSwapper != null && _toolSwapper.IsSwapped)
            {
                _toolSwapper.SwapOut();

                if (_playerAnimator != null && _animator != null)
                {
                    AnimatorOverrideController weaponController = _playerAnimator.ResolveCurrentWeaponController();
                    if (weaponController != null && _animator.runtimeAnimatorController != weaponController)
                        _animator.runtimeAnimatorController = weaponController;
                }
            }

            IsCompleted = true;
            Completed?.Invoke(this);
        }

        private void FacingTarget()
        {
            if (_target == null || _target.Transform == null) return;

            Vector3 toTarget = _target.Transform.position - _controller.PlayerTransform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.001f) return;

            _controller.PlayerTransform.rotation = Quaternion.LookRotation(toTarget, Vector3.up);
        }
    }
}