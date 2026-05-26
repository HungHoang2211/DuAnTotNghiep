using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.Items
{
    /// <summary>
    /// Connects cell hold events to the item info panel. When the player holds
    /// a cell that contains an item, the panel appears beside it; releasing the
    /// hold hides it again. Empty cells show nothing.
    /// </summary>
    public sealed class InventoryInfoController : MonoBehaviour
    {
        [SerializeField] private ItemInfoPanel infoPanel;

        [Tooltip("Every SlotUI that can show item info. Leave empty to auto-collect.")]
        [SerializeField] private List<SlotUI> cells = new List<SlotUI>();

        [SerializeField] private bool autoCollectFromChildren = true;

        private void Awake()
        {
            if (cells.Count == 0 && autoCollectFromChildren)
            {
                cells.Clear();
                cells.AddRange(GetComponentsInChildren<SlotUI>(includeInactive: true));
            }
        }

        private void OnEnable()
        {
            foreach (SlotUI cell in cells)
            {
                cell.OnHeld += HandleCellHeld;
                cell.OnReleased += HandleCellReleased;
            }
        }

        private void OnDisable()
        {
            foreach (SlotUI cell in cells)
            {
                cell.OnHeld -= HandleCellHeld;
                cell.OnReleased -= HandleCellReleased;
            }
        }

        private void HandleCellHeld(SlotUI cell)
        {
            if (!cell.HasItem)
            {
                return;
            }

            RectTransform cellRect = cell.transform as RectTransform;
            infoPanel.Show(cell.CurrentStack, cellRect);
        }

        private void HandleCellReleased(SlotUI cell)
        {
            infoPanel.Hide();
        }
    }
}