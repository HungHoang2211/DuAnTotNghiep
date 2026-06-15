using SimpleSurvival.Items;
using SimpleSurvival.Player;
using SimpleSurvival.Targets;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SimpleSurvival.UI
{
    public class UseButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("References")]
        [SerializeField] private PlayerActionController actionController;
        [SerializeField] private PlayerTargetChecker targetChecker;
        [SerializeField] private PlayerInventoryQueries inventoryQueries;
        [SerializeField] private Transform pressRoot;
        [SerializeField] private Animator animator;
        [SerializeField] private Image iconImage;

        [Header("Icons")]
        [SerializeField] private Sprite defaultIcon;
        [SerializeField] private Sprite pickupIcon;
        [SerializeField] private Sprite axeIcon;
        [SerializeField] private Sprite pickaxeIcon;

        private static readonly int ShowTrigger = Animator.StringToHash("Show");
        private static readonly int HideTrigger = Animator.StringToHash("Hide");

        private ITargetable _currentTarget;
        private bool _isActive;

        private void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>();

            ApplyIcon(null);
        }

        private void OnEnable()
        {
            if (targetChecker != null)
                targetChecker.OnUsableChanged += HandleTargetChanged;

            ITargetable initial = targetChecker != null ? targetChecker.CurrentUsable : null;
            HandleTargetChanged(initial);

            // Restore animator state khi button re-enable
            SetAnimatorState();
        }

        private void OnDisable()
        {
            if (targetChecker != null)
                targetChecker.OnUsableChanged -= HandleTargetChanged;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_currentTarget == null) return;
            if (pressRoot != null) pressRoot.localScale = Vector3.one * 0.9f;

            if (_currentTarget is HarvestTarget harvest)
            {
                actionController.SetGatherHeld(true);
                DispatchHarvest(harvest);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (pressRoot != null) pressRoot.localScale = Vector3.one;

            actionController.SetGatherHeld(false);

            if (_currentTarget == null) return;

            if (_currentTarget is PickupTarget pickup)
                actionController.RequestPickup(pickup);
        }

        private void HandleTargetChanged(ITargetable target)
        {
            _currentTarget = target;
            SetActive(target != null);
            ApplyIcon(target);
        }

        private void SetActive(bool state)
        {
            if (_isActive == state) return;
            _isActive = state;
            SetAnimatorState();
        }

        private void SetAnimatorState()
        {
            if (animator == null) return;
            animator.SetTrigger(_isActive ? ShowTrigger : HideTrigger);
        }

        private void ApplyIcon(ITargetable target)
        {
            if (iconImage == null) return;

            if (target == null)
            {
                iconImage.sprite = defaultIcon;
                return;
            }

            switch (target)
            {
                case PickupTarget _:
                    iconImage.sprite = pickupIcon;
                    break;
                case HarvestTarget harvest:
                    iconImage.sprite = harvest.RequiredTool == ToolType.Pickaxe ? pickaxeIcon : axeIcon;
                    break;
                default:
                    iconImage.sprite = defaultIcon;
                    break;
            }
        }

        private void DispatchHarvest(HarvestTarget harvest)
        {
            if (inventoryQueries == null) return;

            if (!inventoryQueries.HasTool(harvest.RequiredTool))
            {
                bool equippedHasTool = HasEquippedTool(harvest.RequiredTool);
                if (!equippedHasTool)
                {
                    Debug.Log($"[UseButton] Missing tool: {harvest.RequiredTool}");
                    return;
                }
            }

            actionController.RequestGather(harvest);
        }

        private bool HasEquippedTool(ToolType required)
        {
            if (actionController == null) return false;

            var equipment = actionController.GetComponentInChildren<SimpleSurvival.Items.PlayerEquipment>();
            if (equipment == null || equipment.System == null) return false;

            var stack = equipment.System.GetSlot(SimpleSurvival.Items.EquipSlot.Weapon, 0);
            if (stack == null) return false;

            var tool = stack.ItemData.GetAbility<SimpleSurvival.Items.ToolAbility>();
            return tool != null && tool.ToolType == required;
        }
    }
}