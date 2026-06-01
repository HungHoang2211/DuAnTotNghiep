using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SimpleSurvival.Items
{
    /// <summary>
    /// Manages the drag ghost and resolves drop targets. Each SlotUI fires its
    /// own drag events; this controller listens to all of them and handles:
    ///   - showing/moving the ghost image while dragging
    ///   - finding the destination cell on drop
    ///   - calling TransferOrSwap between inventories
    /// </summary>
    public sealed class InventoryDragController : MonoBehaviour
    {
        [Header("Grids")]
        [Tooltip("All grids whose cells can participate in drag-drop.")]
        [SerializeField] private List<InventoryGridUI> grids = new List<InventoryGridUI>();

        [Header("Drag Ghost")]
        [SerializeField] private Image dragGhost;

        [SerializeField] private RectTransform canvasRect;

        [Tooltip("Camera rendering the Canvas. Required for Screen Space - Camera.")]
        [SerializeField] private Camera uiCamera;

        private SlotUI sourceCell;
        private InventoryGridUI sourceGrid;
        private int sourceIndex;

        public bool IsDragging => sourceCell != null;

        // ── Unity lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            if (dragGhost != null)
            {
                dragGhost.gameObject.SetActive(false);
                dragGhost.raycastTarget = false;
            }
        }

        private void OnEnable()
        {
            foreach (InventoryGridUI grid in grids)
            {
                foreach (SlotUI cell in grid.GetComponentsInChildren<SlotUI>(true))
                    SubscribeCell(cell);
            }
        }

        private void OnDisable()
        {
            foreach (InventoryGridUI grid in grids)
            {
                foreach (SlotUI cell in grid.GetComponentsInChildren<SlotUI>(true))
                    UnsubscribeCell(cell);
            }
        }

        // ── Subscription helpers ─────────────────────────────────────────────

        private void SubscribeCell(SlotUI cell)
        {
            cell.OnBeginDragEvent += HandleBeginDrag;
            cell.OnDragEvent += HandleDrag;
            cell.OnEndDragEvent += HandleEndDrag;
            cell.OnDropEvent += HandleDrop;
        }

        private void UnsubscribeCell(SlotUI cell)
        {
            cell.OnBeginDragEvent -= HandleBeginDrag;
            cell.OnDragEvent -= HandleDrag;
            cell.OnEndDragEvent -= HandleEndDrag;
            cell.OnDropEvent -= HandleDrop;
        }

        // ── Drag handlers ────────────────────────────────────────────────────

        private void HandleBeginDrag(SlotUI cell, PointerEventData eventData)
        {
            if (!TryFindCellLocation(cell, out InventoryGridUI grid, out int index))
                return;

            sourceCell = cell;
            sourceGrid = grid;
            sourceIndex = index;

            dragGhost.sprite = cell.CurrentStack.ItemData.Icon;
            dragGhost.gameObject.SetActive(true);
            MoveGhost(eventData.position);
        }

        private void HandleDrag(SlotUI cell, PointerEventData eventData)
        {
            if (!IsDragging)
                return;

            MoveGhost(eventData.position);
        }

        private void HandleEndDrag(SlotUI cell, PointerEventData eventData)
        {
            EndDrag();
        }

        /// <summary>
        /// Called on the DESTINATION cell when a drag is dropped onto it.
        /// </summary>
        private void HandleDrop(SlotUI targetCell)
        {
            if (!IsDragging)
                return;

            if (TryFindCellLocation(targetCell, out InventoryGridUI targetGrid, out int targetIndex))
            {
                InventorySystem.TransferOrSwap(
                    sourceGrid.BoundInventory, sourceIndex,
                    targetGrid.BoundInventory, targetIndex);
            }

            EndDrag();
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private void MoveGhost(Vector2 screenPoint)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPoint, uiCamera, out Vector2 localPoint);

            ((RectTransform)dragGhost.transform).anchoredPosition = localPoint;
        }

        private void EndDrag()
        {
            sourceCell = null;
            sourceGrid = null;
            sourceIndex = -1;
            dragGhost.gameObject.SetActive(false);
        }

        private bool TryFindCellLocation(SlotUI cell, out InventoryGridUI grid, out int index)
        {
            foreach (InventoryGridUI candidate in grids)
            {
                int found = candidate.IndexOf(cell);
                if (found >= 0)
                {
                    grid = candidate;
                    index = found;
                    return true;
                }
            }

            grid = null;
            index = -1;
            return false;
        }
    }
}