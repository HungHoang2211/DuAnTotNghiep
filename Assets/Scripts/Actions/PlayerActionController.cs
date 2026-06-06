using System;
using UnityEngine;
using SimpleSurvival.Actions;
using SimpleSurvival.Targets;
using SimpleSurvival.Items;
using SimpleSurvival.Stats;

namespace SimpleSurvival.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerActionController : MonoBehaviour
    {
        [SerializeField] private MoveActionConfig moveConfig = new MoveActionConfig();

        [Header("Combat References")]
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private PlayerEquipment playerEquipment;

        [Header("Combat Settings")]
        [SerializeField] private float baseAttackRange = 1.5f;

        public IAction CurrentAction { get; private set; }
        public event Action<IAction, IAction> OnActionChanged;

        public CharacterController Controller { get; private set; }
        public Transform PlayerTransform { get; private set; }

        private IdleAction _idleAction;
        private MoveAction _moveAction;

        private void Awake()
        {
            Controller = GetComponent<CharacterController>();
            PlayerTransform = transform;

            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (playerStats == null) playerStats = GetComponent<PlayerStats>();
            if (playerEquipment == null) playerEquipment = GetComponent<PlayerEquipment>();

            _idleAction = new IdleAction(this);
            _moveAction = new MoveAction(this, moveConfig);

            CurrentAction = _idleAction;
            CurrentAction.Init();
        }

        private void Update()
        {
            CurrentAction.Update(Time.deltaTime);

            if (CurrentAction.IsCompleted)
                SwitchToIdle();
        }

        public bool TryRequestAction(IAction newAction)
        {
            if (newAction == null) return false;
            if (!CurrentAction.CanBeInterruptedBy(newAction)) return false;

            SwitchAction(newAction);
            return true;
        }

        public void RequestMove(Vector3 worldDirection, float magnitude, bool sneakHeld)
        {
            _moveAction.UpdateInput(worldDirection, magnitude, sneakHeld);

            if (CurrentAction == _moveAction) return;
            if (magnitude < 0.1f) return;

            if (!CurrentAction.CanBeInterruptedBy(_moveAction)) return;
            SwitchAction(_moveAction);
        }

        public bool RequestAttack(ITargetable target)
        {
            if (target == null || !target.CanBeTargeted()) return false;
            if (animator == null || playerStats == null) return false;

            float damage = playerStats.BaseDamage;
            float range = baseAttackRange;

            AttackAction attack = new AttackAction(this, animator, playerStats, target, damage, range);
            return TryRequestAction(attack);
        }

        public void ForceIdle()
        {
            SwitchToIdle();
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