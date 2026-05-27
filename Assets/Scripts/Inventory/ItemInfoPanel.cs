using System.Text;
using UnityEngine;
using TMPro;

namespace SimpleSurvival.Items
{
    /// <summary>
    /// A floating tooltip that shows an item's details. The caption shows the
    /// item name; the body shows the description followed by ability stats for
    /// equipment, tools, weapons and consumables. A resource with no abilities
    /// shows only its description.
    ///
    /// The panel appears next to the held cell and flips to the other side
    /// when the cell is near a screen edge, so it never runs off-screen.
    /// Hidden by default; Show/Hide are called by the inventory input code.
    /// </summary>
    public sealed class ItemInfoPanel : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("CanvasGroup of the tooltip — its alpha shows/hides the panel.")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform panelRect;

        [Tooltip("Caption text — shows the item name.")]
        [SerializeField] private TMP_Text captionText;

        [Tooltip("Body text — shows description and, for equipment, the stats.")]
        [SerializeField] private TMP_Text bodyText;

        [Header("Placement")]
        [Tooltip("Horizontal offset from the cell to the panel, in pixels.")]
        [SerializeField] private float horizontalOffset = 20f;

        [Tooltip("Canvas this panel lives on — needed to convert positions.")]
        [SerializeField] private RectTransform canvasRect;

        [Tooltip("Camera rendering the Canvas. Required for Screen Space - Camera; "
            + "leave empty for Screen Space - Overlay.")]
        [SerializeField] private Camera uiCamera;

        private void Awake()
        {
            Hide();
        }

        /// <summary>
        /// Shows the panel for the given stack, positioned beside the cell.
        /// Does nothing if the stack is null.
        /// </summary>
        public void Show(ItemStack stack, RectTransform cellRect)
        {
            if (stack == null)
            {
                return;
            }

            captionText.text = stack.ItemData.ItemName;
            bodyText.text = BuildBody(stack.ItemData);

            PositionBeside(cellRect);
            SetVisible(true);
        }

        public void Hide()
        {
            SetVisible(false);
        }

        /// <summary>
        /// Shows or hides the tooltip through its CanvasGroup. Alpha controls
        /// visibility; blocksRaycasts is cleared when hidden so the invisible
        /// panel does not silently swallow clicks.
        /// </summary>
        private void SetVisible(bool visible)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.blocksRaycasts = visible;
        }

        /// <summary>
        /// Builds the body text: the description, then a blank line and the
        /// ability stats if the item has any. A resource yields just the
        /// description.
        /// </summary>
        private string BuildBody(ItemData item)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(item.Description);

            string stats = BuildStats(item);
            if (stats.Length > 0)
            {
                builder.AppendLine();
                builder.AppendLine();
                builder.Append(stats);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Builds the stats block by asking the item for each ability type.
        /// An item without abilities (a resource) yields an empty string.
        /// </summary>
        private string BuildStats(ItemData item)
        {
            StringBuilder builder = new StringBuilder();

            WeaponAbility weapon = item.GetAbility<WeaponAbility>();
            if (weapon != null)
            {
                builder.AppendLine($"Damage: {weapon.Damage}");
                builder.AppendLine($"Attack Speed: {weapon.AttackSpeed}");
            }

            ToolAbility tool = item.GetAbility<ToolAbility>();
            if (tool != null)
            {
                builder.AppendLine($"Tool: {tool.ToolType}");
            }

            EquipmentAbility equipment = item.GetAbility<EquipmentAbility>();
            if (equipment != null)
            {
                builder.AppendLine($"Slot: {equipment.EquipSlot}");
                if (equipment.ArmorValue > 0f)
                {
                    builder.AppendLine($"Armor: {equipment.ArmorValue}");
                }
            }

            ContainerAbility container = item.GetAbility<ContainerAbility>();
            if (container != null)
            {
                builder.AppendLine($"Extra Slots: {container.ExtraSlots}");
            }

            ConsumableAbility consumable = item.GetAbility<ConsumableAbility>();
            if (consumable != null)
            {
                AppendIfNonZero(builder, "Restores HP", consumable.RestoreHp);
                AppendIfNonZero(builder, "Restores Hunger", consumable.RestoreHunger);
                AppendIfNonZero(builder, "Restores Thirst", consumable.RestoreThirst);
            }

            if (item.IsDurable)
            {
                builder.AppendLine($"Max Durability: {item.MaxDurability}");
            }

            return builder.ToString().TrimEnd();
        }

        private static void AppendIfNonZero(StringBuilder builder, string label, float value)
        {
            if (value > 0f)
            {
                builder.AppendLine($"{label}: {value}");
            }
        }

        /// <summary>
        /// Places the panel beside the cell, in the Canvas's local space so it
        /// works with Screen Space - Camera. The panel sits to the right of the
        /// cell but flips left when the cell is past the Canvas mid-line, and
        /// is clamped so it always stays fully inside the Canvas.
        /// </summary>
        private void PositionBeside(RectTransform cellRect)
        {
            // Convert the cell's centre to a point in the Canvas's local space.
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
                uiCamera, cellRect.position);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPoint, uiCamera, out Vector2 cellLocal);

            // Sizes are in Canvas local units (no lossyScale needed here).
            float cellHalfWidth = cellRect.rect.width * 0.5f;
            float panelHalfWidth = panelRect.rect.width * 0.5f;
            float panelHalfHeight = panelRect.rect.height * 0.5f;

            // Flip to the left side when the cell is past the Canvas centre.
            bool placeLeft = cellLocal.x > 0f;
            float direction = placeLeft ? -1f : 1f;
            float offsetX = direction * (cellHalfWidth + panelHalfWidth + horizontalOffset);

            Vector2 target = new Vector2(cellLocal.x + offsetX, cellLocal.y);

            // Keep the whole panel inside the Canvas bounds.
            float canvasHalfWidth = canvasRect.rect.width * 0.5f;
            float canvasHalfHeight = canvasRect.rect.height * 0.5f;

            target.x = Mathf.Clamp(target.x,
                -canvasHalfWidth + panelHalfWidth, canvasHalfWidth - panelHalfWidth);
            target.y = Mathf.Clamp(target.y,
                -canvasHalfHeight + panelHalfHeight, canvasHalfHeight - panelHalfHeight);

            panelRect.anchoredPosition = target;
        }
    }
}