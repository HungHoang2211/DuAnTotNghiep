using System;
using UnityEngine;
using SimpleSurvival.Combat;
using SimpleSurvival.Player;
using SimpleSurvival.Targets;

namespace SimpleSurvival.Actions
{
    public class AttackAction : IAction
    {
        private static readonly int ParamAttack = Animator.StringToHash("Attack");
        private static readonly int ParamActionIndex = Animator.StringToHash("ActionIndex");

        public ActionType Type => ActionType.Attack;
        public bool IsCompleted { get; private set; }
        public event Action<IAction> Completed;

        private enum Phase
        {
            Attacking,
            ComboWindow
        }

        private readonly PlayerActionController _controller;
        private readonly Animator _animator;
        private readonly ITargetable _target;
        private readonly float _damage;
        private readonly float _range;
        private readonly int _maxComboIndex;
        private readonly float _comboWindowSeconds;

        private Phase _phase;
        private int _comboIndex;
        private bool _hitAppliedThisSwing;
        private float _comboWindowRemaining;

        public AttackAction(
            PlayerActionController controller,
            Animator animator,
            ITargetable target,
            float damage,
            float range,
            int maxComboIndex,
            float comboWindowSeconds)
        {
            _controller = controller;
            _animator = animator;
            _target = target;
            _damage = damage;
            _range = range;
            _maxComboIndex = Mathf.Max(0, maxComboIndex);
            _comboWindowSeconds = Mathf.Max(0f, comboWindowSeconds);
        }

        public bool CanBeInterruptedBy(IAction newAction)
        {
            if (newAction.Type == ActionType.Move) return false;
            return true;
        }

        public void Init()
        {
            _controller.ConsumeAttackQueue();
            _controller.CancelSneak();
            _comboIndex = 0;
            StartSwing();
        }

        public void Update(float deltaTime)
        {
            if (_phase != Phase.ComboWindow) return;

            _comboWindowRemaining -= deltaTime;

            if (_controller.IsAttackHeld || _controller.AttackInputQueued)
            {
                _controller.ConsumeAttackQueue();
                AdvanceComboIndex();
                StartSwing();
                return;
            }

            if (_comboWindowRemaining <= 0f)
                CompleteAction();
        }

        public void Cancel()
        {
            CompleteAction();
        }

        public void HandleHit()
        {
            if (_hitAppliedThisSwing) return;
            _hitAppliedThisSwing = true;

            if (_target == null || !_target.CanBeTargeted()) return;

            float distance = Vector3.Distance(
                _controller.PlayerTransform.position,
                _target.Transform.position);
            if (distance > _range + _target.Radius) return;

            MonoBehaviour targetMb = _target as MonoBehaviour;
            if (targetMb == null) return;

            IDamageable damageable = targetMb.GetComponent<IDamageable>();
            if (damageable == null || damageable.IsDead) return;

            damageable.TakeDamage(_damage, _controller.gameObject);
        }

        public void HandleEnd()
        {
            _phase = Phase.ComboWindow;
            _comboWindowRemaining = _comboWindowSeconds;
        }

        private void AdvanceComboIndex()
        {
            int next = _comboIndex + 1;
            _comboIndex = next > _maxComboIndex ? 0 : next;
        }

        private void StartSwing()
        {
            FacingTarget();
            _hitAppliedThisSwing = false;
            _phase = Phase.Attacking;
            _animator.SetInteger(ParamActionIndex, _comboIndex);
            _animator.SetTrigger(ParamAttack);
        }

        private void CompleteAction()
        {
            if (IsCompleted) return;
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