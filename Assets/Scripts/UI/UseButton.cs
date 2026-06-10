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
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image iconImage;

        [Header("Icons")]
        [SerializeField] private Sprite defaultIcon;
        [SerializeField] private Sprite pickupIcon;
        [SerializeField] private Sprite axeIcon;
        [SerializeField] private Sprite pickaxeIcon;

        [Header("Visual States")]
        [SerializeField, Range(0f, 1f)] private float availableAlpha = 1f;
        [SerializeField, Range(0f, 1f)] private float unavailableAlpha = 0.4f;
        [SerializeField] private float fadeSpeed = 8f;

        private ITargetable _currentTarget;
        private float _currentAlpha;

        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            _currentAlpha = unavailableAlpha;
            ApplyAlpha();
            ApplyIcon(null);
        }

        private void OnEnable()
        {
            if (targetChecker != null)
                targetChecker.OnUsableChanged += HandleTargetChanged;

            HandleTargetChanged(targetChecker != null ? targetChecker.CurrentUsable : null);
        }

        private void OnDisable()
        {
            if (targetChecker != null)
                targetChecker.OnUsableChanged -= HandleTargetChanged;
        }

        private void Update()
        {
            UpdateAlphaFade();
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
            ApplyIcon(target);
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

        private void UpdateAlphaFade()
        {
            if (canvasGroup == null) return;

            float target = _currentTarget != null ? availableAlpha : unavailableAlpha;
            if (Mathf.Approximately(_currentAlpha, target)) return;

            _currentAlpha = Mathf.MoveTowards(_currentAlpha, target, fadeSpeed * Time.deltaTime);
            ApplyAlpha();
        }

        private void ApplyAlpha()
        {
            if (canvasGroup != null)
                canvasGroup.alpha = _currentAlpha;
        }
    }
}