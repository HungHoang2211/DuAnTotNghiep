using System.Text;
using UnityEngine;
using TMPro;

namespace SimpleSurvival.Items
{
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

        private string BuildStats(ItemData item)
        {
            StringBuilder sb = new StringBuilder();

            WeaponAbility weapon = item.GetAbility<WeaponAbility>();
            if (weapon != null)
            {
                sb.AppendLine($"Damage: {weapon.Damage}");
                sb.AppendLine($"Attack Speed: {weapon.AttackSpeed}");
            }

            ToolAbility tool = item.GetAbility<ToolAbility>();
            if (tool != null)
            {
                sb.AppendLine($"Tool: {tool.ToolType}");
            }

            EquipmentAbility equipment = item.GetAbility<EquipmentAbility>();
            if (equipment != null)
            {
                if (equipment.ArmorValue > 0f)
                    sb.AppendLine($"Armor: {equipment.ArmorValue}");

                if (equipment.SpeedBonus > 0f)
                    sb.AppendLine($"Speed: +{equipment.SpeedBonus * 100f:F0}%");
            }

            return sb.ToString().TrimEnd();
        }

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