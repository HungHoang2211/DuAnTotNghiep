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
        private readonly HarvestTarget _target;
        private readonly float _damage;

        private bool _hitAppliedThisChop;
        private bool _targetDepleted;

        public GatherAction(
            PlayerActionController controller,
            Animator animator,
            PlayerInventoryQueries inventoryQueries,
            HarvestTarget target,
            float damage)
        {
            _controller = controller;
            _animator = animator;
            _inventoryQueries = inventoryQueries;
            _target = target;
            _damage = damage;
        }

        public bool CanBeInterruptedBy(IAction newAction)
        {
            if (newAction.Type == ActionType.Move) return false;
            return true;
        }

        public void Init()
        {
            _controller.CancelSneak();
            _target.Stats.OnDepleted += HandleTargetDepleted;
            StartChop();
        }

        public void Update(float deltaTime) { }

        public void Cancel()
        {
            CompleteAction();
        }

        public void HandleHit()
        {
            if (_hitAppliedThisChop) return;
            _hitAppliedThisChop = true;

            if (_target == null || _target.Stats == null) return;
            if (_target.Stats.IsDepleted) return;

            FacingTarget();

            _target.Stats.TakeDamage(_damage);
        }

        public void HandleEnd()
        {
            if (_targetDepleted)
            {
                DropItems();
                CompleteAction();
                return;
            }

            if (_controller.IsGatherHeld)
            {
                StartChop();
                return;
            }

            CompleteAction();
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
            FacingTarget();
            _hitAppliedThisChop = false;
            _animator.SetTrigger(ParamGather);
        }

        private void CompleteAction()
        {
            if (IsCompleted) return;

            if (_target != null && _target.Stats != null)
                _target.Stats.OnDepleted -= HandleTargetDepleted;

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