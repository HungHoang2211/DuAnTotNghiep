using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

namespace SimpleSurvival.Items
{
    /// <summary>
    /// Displays the item currently equipped in one slot and handles
    /// player interaction (click, double-click, drag out, drop in).
    /// Ghost và drop logic được xử lý bởi InventoryDragController.
    /// </summary>
    public sealed class EquipSlotUI : MonoBehaviour,
        IDropHandler, IPointerClickHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Slot Config")]
        [SerializeField] private EquipSlot equipSlot;

        [Header("Visuals")]
        [SerializeField] private Image icon;
        [SerializeField] private Sprite defaultIcon;

        [Header("Highlights")]
        [SerializeField] private GameObject selectionHighlight;
        [SerializeField] private GameObject dragHighlight;
        [SerializeField] private GameObject durabilityBar;
        [SerializeField] private Image durabilityLevel;
        [SerializeField] private TMP_Text countText;

        [Header("Durability Colors")]
        [SerializeField] private Color durabilityNormalColor = new Color(0.149f, 0.380f, 0.376f);
        [SerializeField] private Color durabilityLowColor = new Color(0.85f, 0.2f, 0.2f);
        [SerializeField, Range(0f, 1f)] private float lowDurabilityThreshold = 0.25f;

        private ItemStack _equippedStack;
        private float _lastClickTime;
        private const float DoubleClickThreshold = 0.25f;


        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            if (selectionHighlight != null) selectionHighlight.SetActive(false);
            if (dragHighlight != null) dragHighlight.SetActive(false);
        }

        // ── Events ───────────────────────────────────────────────────────────

        public event Action<EquipSlotUI> OnClicked;
        public event Action<EquipSlotUI> OnDoubleClicked;
        public event Action<EquipSlotUI, PointerEventData> OnBeginDragEvent;
        public event Action<EquipSlotUI, PointerEventData> OnDragEvent;
        public event Action<EquipSlotUI, PointerEventData> OnEndDragEvent;
        /// <summary>Raised khi có item được drop lên slot này.</summary>
        public event Action<EquipSlotUI> OnDropEvent;

        // ── Properties ───────────────────────────────────────────────────────

        public EquipSlot EquipSlot => equipSlot;
        public ItemStack EquippedStack => _equippedStack;
        public bool HasItem => _equippedStack != null;

        // ── Public API ───────────────────────────────────────────────────────

        public void SetStack(ItemStack stack)
        {
            _equippedStack = stack;
            if (stack == null) ShowEmpty();
            else ShowItem(stack);
        }

        /// <summary>Selection highlight — shown when player clicks this cell.</summary>
        public void SetSelected(bool selected)
        {
            if (selectionHighlight != null)
                selectionHighlight.SetActive(selected);
        }

        /// <summary>Drag highlight — shown when a compatible item is being dragged.</summary>
        public void SetDragTarget(bool active)
        {
            if (dragHighlight != null)
                dragHighlight.SetActive(active);
        }



        // ── Input handlers ───────────────────────────────────────────────────

        public void OnPointerClick(PointerEventData eventData)
        {
            float now = Time.unscaledTime;
            bool isDouble = now - _lastClickTime < DoubleClickThreshold;
            _lastClickTime = now;

            if (isDouble) OnDoubleClicked?.Invoke(this);
            else OnClicked?.Invoke(this);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!HasItem) return;
            OnBeginDragEvent?.Invoke(this, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            OnDragEvent?.Invoke(this, eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            OnEndDragEvent?.Invoke(this, eventData);
        }

        public void OnDrop(PointerEventData eventData)
        {
            OnDropEvent?.Invoke(this);
        }

        // ── Display helpers ──────────────────────────────────────────────────

        private void ShowEmpty()
        {
            icon.sprite = defaultIcon;
            icon.color = defaultIcon != null ? Color.white : new Color(1f, 1f, 1f, 0f);
            if (countText != null) countText.enabled = false;
            if (durabilityBar != null) durabilityBar.SetActive(false);
        }

        private void ShowItem(ItemStack stack)
        {
            icon.sprite = stack.ItemData.Icon;
            icon.color = Color.white;

            if (countText != null)
            {
                bool showCount = stack.ItemData.IsStackable && stack.Quantity > 1;
                countText.enabled = showCount;
                if (showCount) countText.text = stack.Quantity.ToString();
            }

            if (durabilityBar != null)
            {
                bool isDurable = stack.ItemData.IsDurable;
                durabilityBar.SetActive(isDurable);
                if (isDurable && durabilityLevel != null)
                {
                    float ratio = stack.DurabilityRatio;
                    durabilityLevel.fillAmount = ratio;
                    durabilityLevel.color = ratio < lowDurabilityThreshold
                        ? durabilityLowColor : durabilityNormalColor;
                }
            }
        }
    }
}