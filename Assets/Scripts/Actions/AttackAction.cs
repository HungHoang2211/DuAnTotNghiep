using System;
using UnityEngine;
using SimpleSurvival.Combat;
using SimpleSurvival.Items;
using SimpleSurvival.Player;
using SimpleSurvival.Targets;

namespace SimpleSurvival.Actions
{
    public class AttackAction : IAction
    {
        private static readonly int ParamAttack = Animator.StringToHash("Attack");
        private static readonly int ParamActionIndex = Animator.StringToHash("ActionIndex");
        private static readonly int ParamAttackSpeed = Animator.StringToHash("AttackSpeed");

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
        private readonly PlayerTargetChecker _targetChecker;
        private readonly PlayerToolSwapper _toolSwapper;
        private ITargetable _target;
        private readonly ItemStack _weaponStack;
        private readonly float _damage;
        private readonly float _range;
        private readonly int _maxComboIndex;
        private readonly float _comboWindowSeconds;
        private readonly float _safetyTimeout;
        private readonly float _attackSpeedMultiplier;

        private Phase _phase;
        private int _comboIndex;
        private bool _hitAppliedThisSwing;
        private bool _hitLandedThisSwing;
        private float _comboWindowRemaining;
        private float _swingTimer;
        private bool _weaponBrokeThisSwing;

        public bool WeaponBroke => _weaponBrokeThisSwing;
        public ItemStack WeaponStack => _weaponStack;
        public bool HitLandedThisSwing => _hitLandedThisSwing;

        public AttackAction(
            PlayerActionController controller,
            Animator animator,
            ITargetable target,
            PlayerTargetChecker targetChecker,
            PlayerToolSwapper toolSwapper,
            ItemStack weaponStack,
            float damage,
            float range,
            int maxComboIndex,
            float comboWindowSeconds,
            float safetyTimeout,
            float attackSpeedMultiplier)
        {
            _controller = controller;
            _animator = animator;
            _target = target;
            _targetChecker = targetChecker;
            _toolSwapper = toolSwapper;
            _weaponStack = weaponStack;
            _damage = damage;
            _range = range;
            _maxComboIndex = Mathf.Max(0, maxComboIndex);
            _comboWindowSeconds = Mathf.Max(0f, comboWindowSeconds);
            _safetyTimeout = Mathf.Max(0.1f, safetyTimeout);
            _attackSpeedMultiplier = Mathf.Max(0.1f, attackSpeedMultiplier);
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
            _toolSwapper?.ForceRevertNow();
            _animator.SetFloat(ParamAttackSpeed, _attackSpeedMultiplier);
            PickComboIndex();
            StartSwing();
        }

        public void Update(float deltaTime)
        {
            if (_phase == Phase.Attacking)
            {
                _swingTimer += deltaTime;
                if (_swingTimer >= _safetyTimeout)
                {
                    Debug.LogWarning($"[SafetyTimeout] AttackAction swing exceeded {_safetyTimeout}s. Force HandleEnd. (Missing OnAttackEnd event?)");
                    HandleEnd();
                }
                return;
            }

            _comboWindowRemaining -= deltaTime;

            if (_weaponBrokeThisSwing)
            {
                CompleteAction();
                return;
            }

            if (_controller.IsAttackHeld || _controller.AttackInputQueued)
            {
                _controller.ConsumeAttackQueue();
                RefreshTarget();
                PickComboIndex();
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

            Vector3 playerPos = _controller.PlayerTransform.position;
            float distance;

            if (_target.DistanceCollider != null)
            {
                Vector3 closestPoint = _target.DistanceCollider.ClosestPoint(playerPos);
                distance = Vector3.Distance(playerPos, closestPoint);
            }
            else
            {
                distance = Vector3.Distance(playerPos, _target.Transform.position) - _target.Radius;
                if (distance < 0f) distance = 0f;
            }

            if (distance > _range) return;

            MonoBehaviour targetMb = _target as MonoBehaviour;
            if (targetMb == null) return;

            IDamageable damageable = ResolveDamageable(targetMb);
            if (damageable == null || damageable.IsDead) return;

            damageable.TakeDamage(_damage, _controller.gameObject);
            _hitLandedThisSwing = true;

            ConsumeWeaponDurability();
        }

        public void HandleEnd()
        {
            if (_phase == Phase.ComboWindow) return;
            _phase = Phase.ComboWindow;
            _comboWindowRemaining = _comboWindowSeconds;
        }

        private void RefreshTarget()
        {
            if (_targetChecker == null) return;
            _target = _targetChecker.CurrentEnemy;
        }

        private static IDamageable ResolveDamageable(MonoBehaviour target)
        {
            IDamageable d = target.GetComponent<IDamageable>();
            if (d != null) return d;
            d = target.GetComponentInParent<IDamageable>();
            if (d != null) return d;
            return target.GetComponentInChildren<IDamageable>();
        }

        private void ConsumeWeaponDurability()
        {
            if (_weaponStack == null) return;
            if (!_weaponStack.ItemData.IsDurable) return;

            bool broke = _weaponStack.ReduceDurability();
            if (broke)
            {
                _weaponBrokeThisSwing = true;
                Debug.Log($"[WeaponBroken] {_weaponStack.ItemData.ItemName} broke");
            }
        }

        private void PickComboIndex()
        {
            if (_maxComboIndex <= 0)
            {
                _comboIndex = 0;
                return;
            }

            _comboIndex = UnityEngine.Random.Range(0, _maxComboIndex + 1);
        }

        private void StartSwing()
        {
            FacingTarget();
            _hitAppliedThisSwing = false;
            _hitLandedThisSwing = false;
            _swingTimer = 0f;
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