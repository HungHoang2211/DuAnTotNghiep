using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SimpleSurvival.Items
{
    /// <summary>
    /// Connects cell hold events to the item info panel. When the player holds
    /// a cell that contains an item, the panel appears beside it; releasing the
    /// hold or beginning a drag hides it again. Empty cells show nothing.
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
                cell.OnBeginDragEvent += HandleBeginDrag;
            }
        }

        private void OnDisable()
        {
            foreach (SlotUI cell in cells)
            {
                cell.OnHeld -= HandleCellHeld;
                cell.OnReleased -= HandleCellReleased;
                cell.OnBeginDragEvent -= HandleBeginDrag;
            }
        }

        private void HandleCellHeld(SlotUI cell)
        {
            if (!cell.HasItem)
                return;

            infoPanel.Show(cell.CurrentStack, cell.transform as RectTransform);
        }

        private void HandleCellReleased(SlotUI cell)
        {
            infoPanel.Hide();
        }

        private void HandleBeginDrag(SlotUI cell, PointerEventData eventData)
        {
            infoPanel.Hide();
        }
    }
}