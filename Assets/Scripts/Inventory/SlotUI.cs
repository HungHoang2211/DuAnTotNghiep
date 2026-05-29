using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace SimpleSurvival.Items
{
    /// <summary>
    /// Displays a single inventory cell and reports player input on it. It does
    /// not act on input itself — coordinators (selection, info panel, drag)
    /// listen to its events and decide what happens.
    ///
    /// Input events:
    ///   - OnClicked     : quick tap (press and release before the hold threshold).
    ///   - OnHeld        : press held past the hold threshold, pointer still still.
    ///   - OnDragStart   : after the hold, the pointer began to move — drag begins.
    ///   - OnReleased    : pointer released after a hold that did not become a drag.
    ///
    /// A tap selects; a still hold shows the info tooltip; a hold + move starts a
    /// drag. The two never collide because OnDragStart suppresses tooltip behavior.
    /// </summary>
    public sealed class SlotUI : MonoBehaviour,
        IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
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

        [Header("Hold & Drag")]
        [Tooltip("Seconds the cell must be held before OnHeld fires.")]
        [SerializeField] private float holdThreshold = 0.5f;

        [Tooltip("Pointer movement after hold (pixels) that turns a hold into a drag.")]
        [SerializeField] private float dragMoveThreshold = 8f;

        private bool isLocked;
        private bool isPressed;
        private float pressTime;
        private bool holdFired;
        private bool dragFired;
        private Vector2 pressPosition;

        public event Action<SlotUI> OnClicked;
        public event Action<SlotUI> OnHeld;
        public event Action<SlotUI> OnReleased;

        /// <summary>Raised when a hold turns into a drag (hold + pointer moved).</summary>
        public event Action<SlotUI> OnDragStart;

        public bool HasItem { get; private set; }
        public ItemStack CurrentStack { get; private set; }

        private void Update()
        {
            if (!isPressed)
            {
                return;
            }

            // Fire OnHeld once the press has lasted past the threshold.
            if (!holdFired && Time.unscaledTime - pressTime >= holdThreshold)
            {
                holdFired = true;
                OnHeld?.Invoke(this);
            }

            // After a hold, watch for pointer movement to begin a drag.
            if (holdFired && !dragFired && HasItem)
            {
                Vector2 currentPosition = GetCurrentPointerPosition();
                if (Vector2.Distance(currentPosition, pressPosition) >= dragMoveThreshold)
                {
                    dragFired = true;
                    OnDragStart?.Invoke(this);
                }
            }
        }

        private static Vector2 GetCurrentPointerPosition()
        {
            if (Input.touchCount > 0)
            {
                return Input.GetTouch(0).position;
            }

            return Input.mousePosition;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (isLocked)
            {
                return;
            }

            isPressed = true;
            holdFired = false;
            dragFired = false;
            pressTime = Time.unscaledTime;
            pressPosition = eventData.position;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPressed = false;

            // OnReleased fires only for a still hold that did not become a drag,
            // so the info tooltip knows when to hide. A drag has its own end path.
            if (holdFired && !dragFired)
            {
                OnReleased?.Invoke(this);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // A click that completed a hold (still or dragged) is not a tap.
            if (isLocked || holdFired)
            {
                return;
            }

            OnClicked?.Invoke(this);
        }

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
            {
                return;
            }

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
            {
                quantityText.text = stack.Quantity.ToString();
            }
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