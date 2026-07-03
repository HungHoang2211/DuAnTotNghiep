using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SimpleSurvival.Actions;
using SimpleSurvival.Player;
using SimpleSurvival.Targets;
using SimpleSurvival.Items;

namespace SimpleSurvival.UI
{
    public class AttackButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("References")]
        [SerializeField] private PlayerActionController actionController;
        [SerializeField] private PlayerTargetChecker targetChecker;
        [SerializeField] private PlayerEquipment playerEquipment;
        [SerializeField] private Transform pressRoot;

        [Header("Icon")]
        [SerializeField] private Image icon;
        [SerializeField] private Sprite defaultIcon;

        [Header("Durability")]
        [SerializeField] private Image durabilityFill;
        [SerializeField] private Color durabilityNormalColor = new Color(0.149f, 0.380f, 0.376f);
        [SerializeField] private Color durabilityLowColor = new Color(0.85f, 0.2f, 0.2f);
        [SerializeField, Range(0f, 1f)] private float lowDurabilityThreshold = 0.25f;

        private EquipmentSystem _system;
        private ItemStack _currentWeapon;

        private void OnEnable()
        {
            _system = playerEquipment != null ? playerEquipment.System : null;
            if (_system != null)
                _system.OnSlotChanged += HandleSlotChanged;

            RefreshWeapon(_system != null ? _system.GetSlot(EquipSlot.Weapon, 0) : null);
        }

        private void OnDisable()
        {
            if (_system != null)
                _system.OnSlotChanged -= HandleSlotChanged;
            UnsubscribeWeapon();
        }

        private void HandleSlotChanged(EquipSlot slot, int index, ItemStack stack)
        {
            if (slot != EquipSlot.Weapon || index != 0) return;
            RefreshWeapon(stack);
        }

        private void RefreshWeapon(ItemStack stack)
        {
            UnsubscribeWeapon();
            _currentWeapon = stack;
            SubscribeWeapon();

            if (stack == null)
            {
                ApplyIcon(defaultIcon);
                UpdateDurability(1f);
                return;
            }

            ApplyIcon(stack.ItemData.Icon);

            if (stack.ItemData.IsDurable)
                UpdateDurability(stack.DurabilityRatio);
            else
                UpdateDurability(1f);
        }

        private void SubscribeWeapon()
        {
            if (_currentWeapon != null)
                _currentWeapon.OnChanged += HandleWeaponChanged;
        }

        private void UnsubscribeWeapon()
        {
            if (_currentWeapon != null)
                _currentWeapon.OnChanged -= HandleWeaponChanged;
        }

        private void HandleWeaponChanged(ItemStack stack)
        {
            if (stack != _currentWeapon) return;
            UpdateDurability(stack.ItemData.IsDurable ? stack.DurabilityRatio : 1f);
        }

        private void ApplyIcon(Sprite sprite)
        {
            if (icon == null) return;
            icon.sprite = sprite;
            icon.enabled = sprite != null;
            if (sprite != null) icon.SetNativeSize();
        }

        private void UpdateDurability(float ratio)
        {
            if (durabilityFill == null) return;

            durabilityFill.fillAmount = ratio;
            durabilityFill.color = ratio < lowDurabilityThreshold
                ? durabilityLowColor : durabilityNormalColor;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (pressRoot != null) pressRoot.localScale = Vector3.one * 0.9f;

            actionController.SetAttackHeld(true);

            if (actionController.CurrentAction.Type != ActionType.Attack)
            {
                ITargetable enemy = targetChecker != null ? targetChecker.CurrentEnemy : null;
                actionController.RequestAttack(enemy);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (pressRoot != null) pressRoot.localScale = Vector3.one;

            actionController.SetAttackHeld(false);
        }
    }
}