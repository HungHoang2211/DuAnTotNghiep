using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace SimpleSurvival.Items
{
    /// <summary>
    /// Displays a single inventory cell and reports player input on it.
    /// Uses Unity's built-in drag interfaces so drag and tooltip never conflict.
    ///
    /// Input events:
    ///   - OnClicked   : quick tap/click (no drag, no hold).
    ///   - OnHeld      : pointer held past holdThreshold — tooltip trigger.
    ///   - OnReleased  : pointer released after a still hold — tooltip hide.
    ///   - OnBeginDrag : Unity fires this when pointer moves past pixelDragThreshold.
    ///   - OnDrag      : pointer moving during drag — used to move the ghost.
    ///   - OnEndDrag   : drag finished (dropped or cancelled).
    ///   - OnDrop      : this cell received a drop from another cell.
    /// </summary>
    public sealed class SlotUI : MonoBehaviour,
        IPointerClickHandler, IPointerDownHandler, IPointerUpHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        [Header("Background")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Sprite unlockedBackground;
        [SerializeField] private Sprite lockedBackground;

        [Header("Selection")]
        [SerializeField] private GameObject selectionHighlight;

        [Header("Content")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text quantityText;

        [Header("Durability")]
        [SerializeField] private GameObject durabilityRoot;
        [SerializeField] private Image durabilityFill;

        [Header("Durability Colors")]
        [SerializeField] private Color durabilityNormalColor = new Color(0.149f, 0.380f, 0.376f);
        [SerializeField] private Color durabilityLowColor = new Color(0.85f, 0.2f, 0.2f);

        [Range(0f, 1f)]
        [SerializeField] private float lowDurabilityThreshold = 0.25f;

        [Header("Hold (Tooltip)")]
        [Tooltip("Seconds held before OnHeld fires.")]
        [SerializeField] private float holdThreshold = 0.5f;

        private bool isLocked;
        private bool isPressed;
        private float pressTime;
        private bool holdFired;
        private bool isDragging;
        private float _lastClickTime;
        private const float DoubleClickThreshold = 0.25f;

        // ── Events ───────────────────────────────────────────────────────────

        public event Action<SlotUI> OnDoubleClicked;
        public event Action<SlotUI> OnClicked;
        public event Action<SlotUI> OnHeld;
        public event Action<SlotUI> OnReleased;
        public event Action<SlotUI, PointerEventData> OnBeginDragEvent;
        public event Action<SlotUI, PointerEventData> OnDragEvent;
        public event Action<SlotUI, PointerEventData> OnEndDragEvent;
        public event Action<SlotUI> OnDropEvent;

        public bool HasItem { get; private set; }
        public ItemStack CurrentStack { get; private set; }

        // ── Unity lifecycle ──────────────────────────────────────────────────

        private void Update()
        {
            if (!isPressed || holdFired || isDragging)
                return;

            if (Time.unscaledTime - pressTime >= holdThreshold)
            {
                holdFired = true;
                OnHeld?.Invoke(this);
            }
        }

        // ── Pointer events ───────────────────────────────────────────────────

        public void OnPointerDown(PointerEventData eventData)
        {
            if (isLocked)
                return;

            isPressed = true;
            holdFired = false;
            isDragging = false;
            pressTime = Time.unscaledTime;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPressed = false;

            // Hide tooltip on release if a still hold was active.
            if (holdFired && !isDragging)
                OnReleased?.Invoke(this);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (isLocked || holdFired || isDragging)
                return;

            float now = Time.unscaledTime;
            bool isDouble = now - _lastClickTime < DoubleClickThreshold;
            _lastClickTime = now;

            if (isDouble)
                OnDoubleClicked?.Invoke(this);
            else
                OnClicked?.Invoke(this);
        }

        // ── Drag events (Unity built-in) ─────────────────────────────────────

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (isLocked || !HasItem)
                return;

            isDragging = true;

            // If tooltip was showing, hide it before drag takes over.
            if (holdFired)
                OnReleased?.Invoke(this);

            OnBeginDragEvent?.Invoke(this, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging)
                return;

            OnDragEvent?.Invoke(this, eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDragging)
                return;

            isDragging = false;
            OnEndDragEvent?.Invoke(this, eventData);
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (isLocked)
                return;

            OnDropEvent?.Invoke(this);
        }

        // ── Slot state ───────────────────────────────────────────────────────

        public void SetLocked(bool locked)
        {
            isLocked = locked;
            backgroundImage.sprite = locked ? lockedBackground : unlockedBackground;

            if (locked)
            {
                ShowEmpty();
                SetSelected(false);
            }
        }

        public void SetSelected(bool selected)
        {
            selectionHighlight.SetActive(selected);
        }

        public void SetStack(ItemStack stack)
        {
            if (isLocked)
                return;

            CurrentStack = stack;

            if (stack == null)
            {
                ShowEmpty();
                return;
            }

            HasItem = true;
            ShowIcon(stack);
            ShowQuantity(stack);
            ShowDurability(stack);
        }

        // ── Display helpers ──────────────────────────────────────────────────

        private void ShowEmpty()
        {
            HasItem = false;
            CurrentStack = null;
            iconImage.enabled = false;
            quantityText.enabled = false;
            durabilityRoot.SetActive(false);
        }

        private void ShowIcon(ItemStack stack)
        {
            iconImage.enabled = true;
            iconImage.sprite = stack.ItemData.Icon;
        }

        private void ShowQuantity(ItemStack stack)
        {
            bool showCount = stack.Quantity > 1;
            quantityText.enabled = showCount;

            if (showCount)
                quantityText.text = stack.Quantity.ToString();
        }

        private void ShowDurability(ItemStack stack)
        {
            if (!stack.ItemData.IsDurable)
            {
                durabilityRoot.SetActive(false);
                return;
            }

            durabilityRoot.SetActive(true);

            float ratio = stack.DurabilityRatio;
            durabilityFill.fillAmount = ratio;
            durabilityFill.color = ratio < lowDurabilityThreshold
                ? durabilityLowColor
                : durabilityNormalColor;
        }
    }
} 