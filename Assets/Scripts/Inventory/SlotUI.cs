using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace SimpleSurvival.Items
{
    /// <summary>
    /// Displays a single inventory cell and reports player input on it. It does
    /// not act on input itself — coordinators (InventorySelection, the info
    /// panel) listen to its events and decide what happens.
    ///
    /// Input is reported as three events:
    ///   - OnClicked    : a quick press and release (a tap).
    ///   - OnHeld       : the press was held past the hold threshold.
    ///   - OnReleased   : the pointer was released after a hold.
    /// A tap selects; a hold shows the item info panel. Drag will later also
    /// start from a hold-plus-move, so hold detection is kept separate here.
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

        [Header("Hold")]
        [Tooltip("Seconds the cell must be held before OnHeld fires.")]
        [SerializeField] private float holdThreshold = 0.5f;

        private bool isLocked;
        private bool isPressed;
        private float pressTime;
        private bool holdFired;

        /// <summary>Raised on a quick tap (press and release before the hold threshold).</summary>
        public event Action<SlotUI> OnClicked;

        /// <summary>Raised once when a press is held past the hold threshold.</summary>
        public event Action<SlotUI> OnHeld;

        /// <summary>Raised when the pointer is released after a hold fired.</summary>
        public event Action<SlotUI> OnReleased;

        /// <summary>True when this cell currently holds an item.</summary>
        public bool HasItem { get; private set; }

        /// <summary>The stack shown in this cell, or null when empty.</summary>
        public ItemStack CurrentStack { get; private set; }

        private void Update()
        {
            if (!isPressed || holdFired)
            {
                return;
            }

            if (Time.unscaledTime - pressTime >= holdThreshold)
            {
                holdFired = true;
                OnHeld?.Invoke(this);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (isLocked)
            {
                return;
            }

            isPressed = true;
            holdFired = false;
            pressTime = Time.unscaledTime;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPressed = false;

            if (holdFired)
            {
                OnReleased?.Invoke(this);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // A click that completed a hold is not a tap — ignore it so a hold
            // does not also trigger selection.
            if (isLocked || holdFired)
            {
                return;
            }

            OnClicked?.Invoke(this);
        }

        /// <summary>
        /// Sets whether this cell is locked. A locked cell shows the locked
        /// background, never displays an item, and cannot be selected.
        /// </summary>
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

        /// <summary>Shows or hides the selection highlight on this cell.</summary>
        public void SetSelected(bool selected)
        {
            selectionHighlight.SetActive(selected);
        }

        /// <summary>
        /// Shows the given stack on this cell. Pass null for an empty cell.
        /// Has no effect on a locked cell.
        /// </summary>
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