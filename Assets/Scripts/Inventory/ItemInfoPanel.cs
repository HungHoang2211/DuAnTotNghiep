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
        [SerializeField] private GameObject root;
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

            root.SetActive(true);
            PositionBeside(cellRect);
        }

        public void Hide()
        {
            root.SetActive(false);
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
        /// Places the panel beside the cell. It sits to the right of the cell,
        /// but flips to the left when there is not enough room on the right.
        /// </summary>
        private void PositionBeside(RectTransform cellRect)
        {
            Vector3 cellWorld = cellRect.position;

            bool placeLeft = cellWorld.x > canvasRect.position.x;

            float cellHalfWidth = cellRect.rect.width * 0.5f * cellRect.lossyScale.x;
            float panelHalfWidth = panelRect.rect.width * 0.5f * panelRect.lossyScale.x;

            float direction = placeLeft ? -1f : 1f;
            float offsetX = direction * (cellHalfWidth + panelHalfWidth + horizontalOffset);

            panelRect.position = new Vector3(
                cellWorld.x + offsetX, cellWorld.y, cellWorld.z);
        }
    }
}