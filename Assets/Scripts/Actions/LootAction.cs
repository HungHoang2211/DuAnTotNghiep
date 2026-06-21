using System;
using UnityEngine;
using SimpleSurvival.Player;
using SimpleSurvival.Loot;

namespace SimpleSurvival.Actions
{
    public class LootAction : IAction
    {
        private static readonly int ParamLoot = Animator.StringToHash("Loot");

        public ActionType Type => ActionType.Use;
        public bool IsCompleted { get; private set; }
        public event Action<IAction> Completed;

        private readonly PlayerActionController _controller;
        private readonly Animator _animator;
        private readonly LootContainer _target;
        private readonly Action<LootContainer> _onOpenUI;

        private bool _uiOpened;

        public LootAction(
            PlayerActionController controller,
            Animator animator,
            LootContainer target,
            Action<LootContainer> onOpenUI)
        {
            _controller = controller;
            _animator = animator;
            _target = target;
            _onOpenUI = onOpenUI;
        }

        public bool CanBeInterruptedBy(IAction newAction)
        {
            if (newAction.Type == ActionType.Move) return false;
            return true;
        }

        public void Init()
        {
            _controller.CancelSneak();
            FacingTarget();
            _animator.SetTrigger(ParamLoot);
        }

        public void Update(float deltaTime) { }

        public void Cancel()
        {
            IsCompleted = true;
        }

        public void HandleHit()
        {
            if (_uiOpened) return;
            _uiOpened = true;

            if (_target != null && _target.CanBeTargeted())
            {
                _target.Open();
                _onOpenUI?.Invoke(_target);
            }
        }

        public void HandleEnd()
        {
            IsCompleted = true;
            Completed?.Invoke(this);
        }

        private void FacingTarget()
        {
            if (_target == null) return;

            Vector3 toTarget = _target.Transform.position - _controller.PlayerTransform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.001f) return;

            _controller.PlayerTransform.rotation = Quaternion.LookRotation(toTarget, Vector3.up);
        }
    }
}