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
        [SerializeField] private PlayerToolSwapper toolSwapper;
        [SerializeField] private int moveModeNormal = 0;
        [SerializeField] private int moveModeSneak = 1;
        [SerializeField] private float speedDampTime = 0.1f;

        [Header("Animation")]
        [Tooltip("Override Controller khi không equip weapon (tay không). Drag Fists.overrideController vào đây.")]
        [SerializeField] private AnimatorOverrideController defaultOverrideController;

        private PlayerActionController _actionController;

        public AnimatorOverrideController ResolveCurrentWeaponController()
        {
            ItemStack stack = null;
            if (playerEquipment != null)
                stack = playerEquipment.System.GetSlot(EquipSlot.Weapon, 0);
            return ResolveOverrideController(stack);
        }

        private void Awake()
        {
            _actionController = GetComponent<PlayerActionController>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (inputReader == null) inputReader = GetComponent<PlayerInputReader>();
            if (playerEquipment == null) playerEquipment = GetComponentInChildren<PlayerEquipment>();
            if (toolSwapper == null) toolSwapper = GetComponent<PlayerToolSwapper>();
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
            if (toolSwapper != null && toolSwapper.IsSwapped) return;

            AnimatorOverrideController overrideController = ResolveOverrideController(stack);
            SwapOverrideController(overrideController);
        }

        private AnimatorOverrideController ResolveOverrideController(ItemStack stack)
        {
            if (stack == null) return defaultOverrideController;

            WeaponAbility weapon = stack.ItemData.GetAbility<WeaponAbility>();
            if (weapon != null && weapon.OverrideController != null)
                return weapon.OverrideController;

            ToolAbility tool = stack.ItemData.GetAbility<ToolAbility>();
            if (tool != null && tool.OverrideController != null)
                return tool.OverrideController;

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