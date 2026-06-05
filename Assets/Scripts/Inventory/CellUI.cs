using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace SimpleSurvival.Items
{
    /// <summary>
    /// Unified cell for inventory slots, equipment slots, and loot containers.
    /// Behavior is configured via Inspector fields:
    ///   - equipSlot != None  → equipment cell (drag highlight, default icon)
    ///   - defaultIcon != null → shows placeholder when empty
    ///   - holdThreshold > 0   → enables hold-to-tooltip
    ///   - lockedBackground    → enables lock/unlock (backpack expansion)
    /// </summary>
    public sealed class CellUI : MonoBehaviour,
        IPointerClickHandler, IPointerDownHandler, IPointerUpHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        [Header("Slot Config")]
        [Tooltip("Equipment slot type. Leave as None for inventory/loot cells.")]
        [SerializeField] private EquipSlot equipSlot = EquipSlot.None;

        [Header("Background")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Sprite unlockedBackground;
        [SerializeField] private Sprite lockedBackground;

        [Header("Content")]
        [SerializeField] private Image iconImage;
        [Tooltip("Placeholder shown when empty. Null = hide icon when empty.")]
        [SerializeField] private Sprite defaultIcon;
        [SerializeField] private TMP_Text quantityText;

        [Header("Durability")]
        [SerializeField] private GameObject durabilityRoot;
        [SerializeField] private Image durabilityFill;
        [SerializeField] private Color durabilityNormalColor = new Color(0.149f, 0.380f, 0.376f);
        [SerializeField] private Color durabilityLowColor = new Color(0.85f, 0.2f, 0.2f);
        [SerializeField, Range(0f, 1f)] private float lowDurabilityThreshold = 0.25f;

        [Header("Highlights")]
        [SerializeField] private GameObject selectionHighlight;
        [Tooltip("Shown when a compatible item is dragged over. Equipment cells only.")]
        [SerializeField] private GameObject dragHighlight;

        [Header("Hold (Tooltip)")]
        [Tooltip("Seconds before OnHeld fires. 0 = disabled.")]
        [SerializeField] private float holdThreshold = 0.5f;

        private bool _isLocked;
        private bool _isPressed;
        private float _pressTime;
        private bool _holdFired;
        private bool _isDragging;
        private float _lastClickTime;
        private Color _defaultIconColor;
        private Vector2 _defaultIconSize;

        private const float DoubleClickThreshold = 0.25f;

        // ── Events ───────────────────────────────────────────────────────────

        public event Action<CellUI> OnClicked;
        public event Action<CellUI> OnDoubleClicked;
        public event Action<CellUI> OnHeld;
        public event Action<CellUI> OnReleased;
        public event Action<CellUI, PointerEventData> OnBeginDragEvent;
        public event Action<CellUI, PointerEventData> OnDragEvent;
        public event Action<CellUI, PointerEventData> OnEndDragEvent;
        public event Action<CellUI> OnDropEvent;

        // ── Properties ───────────────────────────────────────────────────────

        public EquipSlot EquipSlot => equipSlot;
        public bool IsEquipCell => equipSlot != EquipSlot.None;
        public bool HasItem { get; private set; }
        public ItemStack CurrentStack { get; private set; }

        // ── Unity lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            if (iconImage != null)
            {
                _defaultIconColor = iconImage.color;
                _defaultIconSize = iconImage.rectTransform.sizeDelta;
            }

            if (selectionHighlight != null) selectionHighlight.SetActive(false);
            if (dragHighlight != null) dragHighlight.SetActive(false);
        }

        private void Update()
        {
            if (!_isPressed || _holdFired || _isDragging || holdThreshold <= 0f)
                return;

            if (Time.unscaledTime - _pressTime >= holdThreshold)
            {
                _holdFired = true;
                OnHeld?.Invoke(this);
            }
        }

        // ── Public API ───────────────────────────────────────────────────────

        public void SetStack(ItemStack stack)
        {
            if (_isLocked) return;

            CurrentStack = stack;

            if (stack == null)
            {
                ShowEmpty();
                return;
            }

            HasItem = true;
            ShowItem(stack);
        }

        public void SetSelected(bool selected)
        {
            if (selectionHighlight != null)
                selectionHighlight.SetActive(selected);
        }

        public void SetDragTarget(bool active)
        {
            if (dragHighlight != null)
                dragHighlight.SetActive(active);
        }

        public void SetLocked(bool locked)
        {
            _isLocked = locked;

            if (backgroundImage != null)
                backgroundImage.sprite = locked ? lockedBackground : unlockedBackground;

            if (locked)
            {
                ShowEmpty();
                SetSelected(false);
            }
        }

        // ── Pointer events ───────────────────────────────────────────────────

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_isLocked) return;

            _isPressed = true;
            _holdFired = false;
            _isDragging = false;
            _pressTime = Time.unscaledTime;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isPressed = false;

            if (_holdFired && !_isDragging)
                OnReleased?.Invoke(this);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_isLocked || _holdFired || _isDragging)
                return;

            float now = Time.unscaledTime;
            bool isDouble = now - _lastClickTime < DoubleClickThreshold;
            _lastClickTime = now;

            if (isDouble) OnDoubleClicked?.Invoke(this);
            else OnClicked?.Invoke(this);
        }

        // ── Drag events ──────────────────────────────────────────────────────

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_isLocked || !HasItem) return;

            _isDragging = true;

            if (_holdFired)
                OnReleased?.Invoke(this);

            OnBeginDragEvent?.Invoke(this, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;
            OnDragEvent?.Invoke(this, eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;
            _isDragging = false;
            OnEndDragEvent?.Invoke(this, eventData);
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (_isLocked) return;
            OnDropEvent?.Invoke(this);
        }

        // ── Display helpers ──────────────────────────────────────────────────

        private void ShowEmpty()
        {
            HasItem = false;
            CurrentStack = null;

            if (defaultIcon != null)
            {
                iconImage.sprite = defaultIcon;
                iconImage.color = _defaultIconColor;
                iconImage.rectTransform.sizeDelta = _defaultIconSize;
                iconImage.enabled = true;
            }
            else
            {
                iconImage.enabled = false;
            }

            if (quantityText != null) quantityText.enabled = false;
            if (durabilityRoot != null) durabilityRoot.SetActive(false);
        }

        private void ShowItem(ItemStack stack)
        {
            iconImage.enabled = true;
            iconImage.sprite = stack.ItemData.Icon;
            iconImage.color = Color.white;
            iconImage.SetNativeSize();

            if (quantityText != null)
            {
                bool showCount = stack.ItemData.IsStackable && stack.Quantity > 1;
                quantityText.enabled = showCount;
                if (showCount) quantityText.text = stack.Quantity.ToString();
            }

            if (durabilityRoot != null)
            {
                bool isDurable = stack.ItemData.IsDurable;
                durabilityRoot.SetActive(isDurable);

                if (isDurable && durabilityFill != null)
                {
                    float ratio = stack.DurabilityRatio;
                    durabilityFill.fillAmount = ratio;
                    durabilityFill.color = ratio < lowDurabilityThreshold
                        ? durabilityLowColor : durabilityNormalColor;
                }
            }
        }
    }
}