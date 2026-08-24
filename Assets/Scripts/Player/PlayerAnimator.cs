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

        [Header("Debug")]
        [Tooltip("Kéo đúng xương R_arm_3_jnt (rightHandAnchor) vào đây để log góc xoay mỗi frame.")]
        [SerializeField] private Transform debugRightHandBone;

        private PlayerActionController _actionController;
        private bool _weaponSlotDirty;
        private ItemStack _pendingWeaponSlotStack;
        private bool _hasPendingWeaponSlotStack;

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

            SwapOverrideController(ResolveCurrentWeaponController());
        }

        private void OnDestroy()
        {
            if (playerEquipment != null && playerEquipment.System != null)
                playerEquipment.System.OnSlotChanged -= HandleSlotChanged;
        }

        private void Update()
        {
            if (animator == null) return;

            float rawMoveSpeed = 0f;
            bool isMovingAction = _actionController.CurrentAction is IMovingAction;
            if (_actionController.CurrentAction is IMovingAction moving)
                rawMoveSpeed = moving.NormalizedSpeed;

            LogFrameCapture(rawMoveSpeed, isMovingAction);

            bool isSneaking = inputReader != null && inputReader.IsSneakHeld;

            animator.SetFloat(ParamMoveSpeed, rawMoveSpeed, speedDampTime, Time.deltaTime);
            animator.SetInteger(ParamMoveMode, isSneaking ? moveModeSneak : moveModeNormal);
        }

        private void LogFrameCapture(float rawMoveSpeed, bool isMovingAction)
        {
            var currentClips = animator.GetCurrentAnimatorClipInfo(0);
            string currentClipName = currentClips.Length > 0 ? currentClips[0].clip.name : "none";
            int clipCount = currentClips.Length;
            bool inTransition = animator.IsInTransition(0);

            float appliedMoveSpeed = animator.GetFloat(ParamMoveSpeed);
            string actionType = _actionController.CurrentAction != null
                ? _actionController.CurrentAction.Type.ToString()
                : "none";

            string line = $"[FrameCap] t={Time.time:F3} f={Time.frameCount} action={actionType} isMovingAction={isMovingAction} rawMoveSpeed={rawMoveSpeed:F3} appliedMoveSpeed={appliedMoveSpeed:F3} currentClip={currentClipName} clipCount={clipCount} inTransition={inTransition}";

            if (clipCount > 1)
            {
                for (int i = 1; i < currentClips.Length; i++)
                    line += $" | extraClip{i}={currentClips[i].clip.name} weight{i}={currentClips[i].weight:F2}";
            }

            if (inTransition)
            {
                var nextClips = animator.GetNextAnimatorClipInfo(0);
                string nextClipName = nextClips.Length > 0 ? nextClips[0].clip.name : "none";
                var transInfo = animator.GetAnimatorTransitionInfo(0);
                line += $" -> nextClip={nextClipName} transNormTime={transInfo.normalizedTime:F2}";
            }

            if (debugRightHandBone != null)
            {
                Vector3 localEuler = debugRightHandBone.localEulerAngles;
                Vector3 worldEuler = debugRightHandBone.eulerAngles;
                line += $" | boneLocalRot=({localEuler.x:F1},{localEuler.y:F1},{localEuler.z:F1}) boneWorldRot=({worldEuler.x:F1},{worldEuler.y:F1},{worldEuler.z:F1})";
            }

            Debug.Log(line);
        }

        private void LateUpdate()
        {
            if (!_weaponSlotDirty) return;
            _weaponSlotDirty = false;

            if (toolSwapper != null && toolSwapper.IsSwapped) return;

            ItemStack stack = _hasPendingWeaponSlotStack
                ? _pendingWeaponSlotStack
                : (playerEquipment != null ? playerEquipment.System.GetSlot(EquipSlot.Weapon, 0) : null);

            AnimatorOverrideController overrideController = ResolveOverrideController(stack);
            SwapOverrideController(overrideController);
        }

        private void HandleSlotChanged(EquipSlot slot, int index, ItemStack stack)
        {
            if (slot != EquipSlot.Weapon) return;

            _weaponSlotDirty = true;
            _pendingWeaponSlotStack = stack;
            _hasPendingWeaponSlotStack = true;
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