using System;
using UnityEngine;
using SimpleSurvival.Player;
using SimpleSurvival.Stats;
using SimpleSurvival.Targets;

namespace SimpleSurvival.Actions
{
    public class AttackAction : IAction
    {
        private static readonly int ParamActionAttackMeleeFists = Animator.StringToHash("ActionAttackMeleeFists");
        private static readonly int ParamActionIndex = Animator.StringToHash("ActionIndex");
        private static readonly int ParamIsInCombat = Animator.StringToHash("IsInCombat");

        public ActionType Type => ActionType.Attack;
        public bool IsCompleted { get; private set; }
        public event Action<IAction> Completed;

        private readonly PlayerActionController _controller;
        private readonly Animator _animator;
        private readonly PlayerStats _playerStats;
        private readonly ITargetable _target;
        private readonly float _damage;
        private readonly float _range;

        private bool _hitApplied;

        public AttackAction(
            PlayerActionController controller,
            Animator animator,
            PlayerStats playerStats,
            ITargetable target,
            float damage,
            float range)
        {
            _controller = controller;
            _animator = animator;
            _playerStats = playerStats;
            _target = target;
            _damage = damage;
            _range = range;
        }

        public bool CanBeInterruptedBy(IAction newAction)
        {
            if (newAction.Type == ActionType.Move) return false;
            return true;
        }

        public void Init()
        {
            FacingTarget();

            //_animator.SetBool(ParamIsInCombat, true);

            int randomIndex = UnityEngine.Random.Range(0, 4);
            _animator.SetFloat(ParamActionIndex, (float)randomIndex);
            _animator.SetTrigger(ParamActionAttackMeleeFists);
        }

        public void Update(float deltaTime) { }

        public void Cancel()
        {
            IsCompleted = true;
        }

        public void HandleHit()
        {
            if (_hitApplied) return;
            _hitApplied = true;

            if (_target == null || !_target.CanBeTargeted()) return;

            float distance = Vector3.Distance(_controller.PlayerTransform.position, _target.Transform.position);
            if (distance > _range + _target.Radius) return;

            MonoBehaviour targetMb = _target as MonoBehaviour;
            if (targetMb == null) return;

            BaseStats stats = targetMb.GetComponent<BaseStats>();
            if (stats != null)
            {
                stats.TakeDamage(_damage);
                return;
            }


            Xyla.Combat.IDamageable damageable = targetMb.GetComponent<Xyla.Combat.IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(_damage, _controller.gameObject);
            }
        }

        public void HandleEnd()
        {
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