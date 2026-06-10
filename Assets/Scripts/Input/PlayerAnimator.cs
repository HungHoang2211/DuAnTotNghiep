using UnityEngine;
using SimpleSurvival.Actions;
using SimpleSurvival.Input;
using SimpleSurvival.Items;

namespace SimpleSurvival.Player
{
    [RequireComponent(typeof(PlayerActionController))]
    public class PlayerAnimator : MonoBehaviour
    {
        private static readonly int ParamMoveSpeed = Animator.StringToHash("MoveSpeed");
        private static readonly int ParamMoveMode = Animator.StringToHash("MoveMode");

        [SerializeField] private Animator animator;
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private PlayerEquipment playerEquipment;
        [SerializeField] private int moveModeNormal = 0;
        [SerializeField] private int moveModeSneak = 1;
        [SerializeField] private float speedDampTime = 0.1f;

        [Header("Animation")]
        [Tooltip("Override Controller khi không equip weapon (tay không). Drag Fists.overrideController vào đây.")]
        [SerializeField] private AnimatorOverrideController defaultOverrideController;

        private PlayerActionController _actionController;

        private void Awake()
        {
            _actionController = GetComponent<PlayerActionController>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (inputReader == null) inputReader = GetComponent<PlayerInputReader>();
            if (playerEquipment == null) playerEquipment = GetComponentInChildren<PlayerEquipment>();
        }

        private void Start()
        {
            if (playerEquipment != null && playerEquipment.System != null)
                playerEquipment.System.OnSlotChanged += HandleSlotChanged;
        }

        private void OnDestroy()
        {
            if (playerEquipment != null && playerEquipment.System != null)
                playerEquipment.System.OnSlotChanged -= HandleSlotChanged;
        }

        private void Update()
        {
            if (animator == null) return;

            float moveSpeed = 0f;

            if (_actionController.CurrentAction is MoveAction move)
                moveSpeed = move.NormalizedSpeed;

            bool isSneaking = inputReader != null && inputReader.IsSneakHeld;

            animator.SetFloat(ParamMoveSpeed, moveSpeed, speedDampTime, Time.deltaTime);
            animator.SetInteger(ParamMoveMode, isSneaking ? moveModeSneak : moveModeNormal);
        }

        private void HandleSlotChanged(EquipSlot slot, int index, ItemStack stack)
        {
            if (slot != EquipSlot.Weapon) return;

            AnimatorOverrideController overrideController = ResolveOverrideController(stack);
            SwapOverrideController(overrideController);
        }

        private AnimatorOverrideController ResolveOverrideController(ItemStack stack)
        {
            if (stack == null) return defaultOverrideController;

            WeaponAbility weapon = stack.ItemData.GetAbility<WeaponAbility>();
            if (weapon != null && weapon.OverrideController != null)
                return weapon.OverrideController;

            return defaultOverrideController;
        }

        private void SwapOverrideController(AnimatorOverrideController overrideController)
        {
            if (animator == null || overrideController == null) return;
            if (animator.runtimeAnimatorController == overrideController) return;

            animator.runtimeAnimatorController = overrideController;
        }
    }
}