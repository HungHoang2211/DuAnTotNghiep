using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using SimpleSurvival.Items;
using SimpleSurvival.Player;

namespace SimpleSurvival.UI
{
    public sealed class QuickSlotButtonHud : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Slot")]
        [SerializeField] private int slotIndex;

        [Header("References")]
        [SerializeField] private PlayerEquipment playerEquipment;
        [SerializeField] private PlayerConsumableHandler consumableHandler;

        [Header("Visual")]
        [SerializeField] private GameObject root;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text quantityText;

        private EquipmentSystem _system;
        private ItemStack _currentStack;

        private void Start()
        {
            _system = playerEquipment != null ? playerEquipment.System : null;

            if (_system != null)
                _system.OnSlotChanged += HandleSlotChanged;

            Refresh();
        }

        private void OnDestroy()
        {
            if (_system != null)
                _system.OnSlotChanged -= HandleSlotChanged;
            UnsubscribeStack();
        }

        private void OnEnable()
        {
            if (_system != null)
                Refresh();
        }

        private void HandleSlotChanged(EquipSlot slot, int index, ItemStack stack)
        {
            if (slot != EquipSlot.QuickSlot || index != slotIndex) return;
            Refresh();
        }

        private void Refresh()
        {
            ItemStack stack = _system != null ? _system.GetSlot(EquipSlot.QuickSlot, slotIndex) : null;

            if (stack != _currentStack)
            {
                UnsubscribeStack();
                _currentStack = stack;
                SubscribeStack();
            }

            if (stack == null)
            {
                if (root != null) root.SetActive(false);
                return;
            }

            if (root != null) root.SetActive(true);

            if (icon != null)
            {
                icon.sprite = stack.ItemData.Icon;
                icon.enabled = true;
            }

            if (quantityText != null)
            {
                bool showCount = stack.ItemData.IsStackable && stack.Quantity > 1;
                quantityText.enabled = showCount;
                if (showCount) quantityText.text = stack.Quantity.ToString();
            }
        }

        private void SubscribeStack()
        {
            if (_currentStack != null)
                _currentStack.OnChanged += HandleStackChanged;
        }

        private void UnsubscribeStack()
        {
            if (_currentStack != null)
                _currentStack.OnChanged -= HandleStackChanged;
        }

        private void HandleStackChanged(ItemStack stack)
        {
            if (stack != _currentStack) return;
            Refresh();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (root != null) root.transform.localScale = Vector3.one * 0.9f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (root != null) root.transform.localScale = Vector3.one;

            if (_currentStack == null || _system == null) return;

            ItemData data = _currentStack.ItemData;

            if (data.HasAbility<WeaponAbility>() || data.HasAbility<ToolAbility>())
                SwapWithWeapon();
            else if (data.HasAbility<ConsumableAbility>())
                UseConsumable();
        }

        private void SwapWithWeapon()
        {
            ItemStack weaponStack = _system.GetSlot(EquipSlot.Weapon, 0);
            ItemStack quickStack = _currentStack;
            int currentSlotIndex = slotIndex;
            EquipmentSystem system = _system;

            void ApplySwap()
            {
                system.SetSlotDirect(EquipSlot.Weapon, 0, quickStack);
                system.SetSlotDirect(EquipSlot.QuickSlot, currentSlotIndex, weaponStack);
            }

            if (PlayerActionController.Instance != null)
                PlayerActionController.Instance.RequestWeaponSlotAction(ApplySwap);
            else
                ApplySwap();
        }

        private void UseConsumable()
        {
            if (consumableHandler == null) return;
            if (!consumableHandler.TryConsume(_currentStack)) return;

            _currentStack.RemoveQuantity(1);
            if (_currentStack.IsEmpty)
                _system.SetSlotDirect(EquipSlot.QuickSlot, slotIndex, null);
        }
    }
}