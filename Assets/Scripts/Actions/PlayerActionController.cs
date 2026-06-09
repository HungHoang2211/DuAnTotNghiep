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
        [SerializeField] private PlayerInventoryQueries inventoryQueries;

        [Header("Combat Defaults (Unarmed)")]
        [SerializeField] private float unarmedAttackRange = 1.5f;
        [SerializeField] private int unarmedMaxComboIndex = 3;
        [SerializeField] private float comboWindowSeconds = 0.25f;

        public IAction CurrentAction { get; private set; }
        public event Action<IAction, IAction> OnActionChanged;

        public CharacterController Controller { get; private set; }
        public Transform PlayerTransform { get; private set; }

        public bool IsAttackHeld { get; private set; }
        public bool AttackInputQueued { get; private set; }

        public void ConsumeAttackQueue()
        {
            AttackInputQueued = false;
        }

        private IdleAction _idleAction;
        private MoveAction _moveAction;

        private void Awake()
        {
            Controller = GetComponent<CharacterController>();
            PlayerTransform = transform;

            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (playerStats == null) playerStats = GetComponentInChildren<PlayerStats>();
            if (playerEquipment == null) playerEquipment = GetComponentInChildren<PlayerEquipment>();
            if (inventoryQueries == null) inventoryQueries = GetComponentInChildren<PlayerInventoryQueries>();

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
            if (animator == null) return false;

            float damage = ResolveAttackDamage();
            float range = ResolveAttackRange();
            int maxComboIndex = ResolveMaxComboIndex();

            AttackAction attack = new AttackAction(
                this, animator, target,
                damage, range, maxComboIndex, comboWindowSeconds);
            return TryRequestAction(attack);
        }

        public void SetAttackHeld(bool held)
        {
            IsAttackHeld = held;
            if (held) AttackInputQueued = true;
        }

        public void ForceIdle()
        {
            SwitchToIdle();
        }

        public bool RequestPickup(PickupTarget target)
        {
            if (target == null || !target.CanBeTargeted()) return false;
            if (animator == null || inventoryQueries == null) return false;

            if (!inventoryQueries.CanAddItem(target.ItemData, target.Quantity))
            {
                Debug.Log("[ActionController] Inventory full, cannot pickup");
                return false;
            }

            PickupAction pickup = new PickupAction(this, animator, inventoryQueries, target);
            return TryRequestAction(pickup);
        }

        private float ResolveAttackDamage()
        {
            WeaponAbility weapon = GetEquippedWeapon();
            if (weapon != null) return weapon.Damage;
            return playerStats != null ? playerStats.BaseDamage : 0f;
        }

        private float ResolveAttackRange()
        {
            WeaponAbility weapon = GetEquippedWeapon();
            if (weapon != null) return weapon.Range;
            return unarmedAttackRange;
        }

        private int ResolveMaxComboIndex()
        {
            WeaponAbility weapon = GetEquippedWeapon();
            if (weapon != null) return weapon.MaxComboIndex;
            return unarmedMaxComboIndex;
        }

        private WeaponAbility GetEquippedWeapon()
        {
            if (playerEquipment == null) return null;
            ItemStack stack = playerEquipment.System.GetSlot(EquipSlot.Weapon, 0);
            if (stack == null) return null;
            return stack.ItemData.GetAbility<WeaponAbility>();
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