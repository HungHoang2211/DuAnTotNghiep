using SimpleSurvival.Items;
using SimpleSurvival.Loot;
using SimpleSurvival.Player;
using SimpleSurvival.Targets;
using SimpleSurvival.UI.Hud;
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
        [SerializeField] private Sprite lootIcon;
        [SerializeField] private Sprite repairIcon;

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
            else if (_currentTarget is LootContainer container)
                actionController.RequestLoot(container);
            else if (_currentTarget is RepairableTower tower)
                actionController.RequestRepairTower(tower);
            else if (_currentTarget is SimpleSurvival.Targets.NPCTargetable npc)
                actionController.RequestNPCInteract(npc);
            else if (_currentTarget is SimpleSurvival.Targets.DogHouseTargetable dogHouse)
                actionController.RequestDogHouseInteract(dogHouse);
            else if (_currentTarget is WitchEventTrap witchTrap)
                actionController.RequestWitchEvent(witchTrap);
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
                case LootContainer _:
                    iconImage.sprite = lootIcon;
                    break;
                case RepairableTower _:
                    iconImage.sprite = repairIcon;
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

                    if (FollowNotifyManager.Instance != null)
                        FollowNotifyManager.Instance.Notify($"Need {harvest.RequiredTool}", SpeechHudType.Bad);

                    return;
                }
            }

            actionController.RequestGather(harvest);
        }

        private bool HasEquippedTool(ToolType required)
        {
            if (actionController == null) return false;

            var equipment = actionController.GetComponentInChildren<PlayerEquipment>();
            if (equipment == null || equipment.System == null) return false;

            if (CheckSlotForTool(equipment.System, EquipSlot.Weapon, 0, required))
                return true;

            if (CheckSlotForTool(equipment.System, EquipSlot.QuickSlot, 0, required))
                return true;

            if (CheckSlotForTool(equipment.System, EquipSlot.QuickSlot, 1, required))
                return true;

            return false;
        }

        private bool CheckSlotForTool(EquipmentSystem system, EquipSlot slot, int index, ToolType required)
        {
            var stack = system.GetSlot(slot, index);
            if (stack == null) return false;

            var tool = stack.ItemData.GetAbility<ToolAbility>();
            return tool != null && tool.ToolType == required;
        }
    }
}