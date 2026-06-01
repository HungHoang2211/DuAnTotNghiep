using System.Text;
using UnityEngine;
using TMPro;

namespace SimpleSurvival.Items
{
    /// <summary>
    /// A floating tooltip that shows an item's details. Caption shows the item
    /// name; body shows the description and, for weapons/tools/equipment only,
    /// a stats block. Consumables and containers show description only.
    /// </summary>
    public sealed class ItemInfoPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform panelRect;
        [SerializeField] private TMP_Text captionText;
        [SerializeField] private TMP_Text bodyText;

        [Header("Placement")]
        [SerializeField] private float horizontalOffset = 20f;
        [SerializeField] private RectTransform canvasRect;
        [SerializeField] private Camera uiCamera;

        private void Awake()
        {
            Hide();
        }

        public void Show(ItemStack stack, RectTransform cellRect)
        {
            if (stack == null)
                return;

            captionText.text = stack.ItemData.ItemName;
            bodyText.text = BuildBody(stack.ItemData);

            PositionBeside(cellRect);
            SetVisible(true);
        }

        public void Hide()
        {
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.blocksRaycasts = visible;
        }

        // ── Body builder ─────────────────────────────────────────────────────

        private string BuildBody(ItemData item)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(item.Description);

            string stats = BuildStats(item);
            if (stats.Length > 0)
            {
                sb.AppendLine();
                sb.AppendLine();
                sb.Append(stats);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Returns a stats block only for weapon, tool, and equipment.
        /// Consumables and containers intentionally return empty string.
        /// </summary>
        private string BuildStats(ItemData item)
        {
            StringBuilder sb = new StringBuilder();

            // Weapon stats — shown for weapons and tools that double as weapons.
            WeaponAbility weapon = item.GetAbility<WeaponAbility>();
            if (weapon != null)
            {
                sb.AppendLine($"Damage: {weapon.Damage}");
                sb.AppendLine($"Attack Speed: {weapon.AttackSpeed}");
            }

            // Tool type — shown alongside weapon stats when item is both.
            ToolAbility tool = item.GetAbility<ToolAbility>();
            if (tool != null)
            {
                sb.AppendLine($"Tool: {tool.ToolType}");
            }

            // Equipment armor — only when item is wearable gear.
            EquipmentAbility equipment = item.GetAbility<EquipmentAbility>();
            if (equipment != null && equipment.ArmorValue > 0f)
            {
                sb.AppendLine($"Armor: {equipment.ArmorValue}");
            }

            return sb.ToString().TrimEnd();
        }

        // ── Positioning ──────────────────────────────────────────────────────

        private void PositionBeside(RectTransform cellRect)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
                uiCamera, cellRect.position);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPoint, uiCamera, out Vector2 cellLocal);

            float cellHalfWidth = cellRect.rect.width * 0.5f;
            float panelHalfWidth = panelRect.rect.width * 0.5f;
            float panelHalfHeight = panelRect.rect.height * 0.5f;

            bool placeLeft = cellLocal.x > 0f;
            float direction = placeLeft ? -1f : 1f;
            float offsetX = direction * (cellHalfWidth + panelHalfWidth + horizontalOffset);

            Vector2 target = new Vector2(cellLocal.x + offsetX, cellLocal.y);

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