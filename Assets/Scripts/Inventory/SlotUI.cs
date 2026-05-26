using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SimpleSurvival.Items
{
    /// <summary>
    /// Displays a single inventory cell. Its only job is to reflect a cell's
    /// state visually — it knows nothing about InventorySystem or drag-drop.
    ///
    /// A cell has two independent aspects:
    ///   - Locked vs unlocked  -> set by SetLocked (depends on the backpack).
    ///   - What stack it holds -> set by SetStack (depends on the item data).
    ///
    /// All UI references are assigned by hand in the Inspector. The script
    /// turns components on and off itself, so they may start disabled.
    /// </summary>
    public sealed class SlotUI : MonoBehaviour
    {
        [Header("Background")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Sprite unlockedBackground;
        [SerializeField] private Sprite lockedBackground;

        [Header("Content")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text quantityText;

        [Header("Durability")]
        [Tooltip("Root object of the durability bar (the background). "
            + "Toggled on/off as a whole — its fill child goes with it.")]
        [SerializeField] private GameObject durabilityRoot;

        [Tooltip("The Filled image inside the durability bar. Image Type must be Filled.")]
        [SerializeField] private Image durabilityFill;

        [Header("Durability Colors")]
        [SerializeField] private Color durabilityNormalColor = new Color(0.149f, 0.380f, 0.376f);
        [SerializeField] private Color durabilityLowColor = new Color(0.85f, 0.2f, 0.2f);

        [Tooltip("Below this ratio (0-1) the durability bar turns to the low color.")]
        [Range(0f, 1f)]
        [SerializeField] private float lowDurabilityThreshold = 0.25f;

        private bool isLocked;

        /// <summary>
        /// Sets whether this cell is locked (not yet unlocked by the backpack).
        /// A locked cell shows the locked background and never displays an item.
        /// </summary>
        public void SetLocked(bool locked)
        {
            isLocked = locked;
            backgroundImage.sprite = locked ? lockedBackground : unlockedBackground;

            if (locked)
            {
                ShowEmpty();
            }
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

            if (stack == null)
            {
                ShowEmpty();
                return;
            }

            ShowIcon(stack);
            ShowQuantity(stack);
            ShowDurability(stack);
        }

        private void ShowEmpty()
        {
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