using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SimpleSurvival.Items
{
    /// <summary>
    /// Coordinates drag-and-drop between inventory grids. A drag starts when a
    /// cell fires OnDragStart (a hold that the pointer moved out of). While the
    /// player drags, an "icon ghost" follows the pointer; on release, the
    /// underlying cell is found and the stacks transfer or swap.
    ///
    /// The original cell stays in place during the drag so the grid layout does
    /// not jump. Only the ghost moves.
    /// </summary>
    public sealed class InventoryDragController : MonoBehaviour
    {
        [Header("Grids")]
        [Tooltip("All grids whose cells can participate in drag-drop.")]
        [SerializeField] private List<InventoryGridUI> grids = new List<InventoryGridUI>();

        [Header("Drag Ghost")]
        [Tooltip("Image shown under the pointer while dragging. Hidden by default.")]
        [SerializeField] private Image dragGhost;

        [Tooltip("Canvas the ghost lives on — needed for screen-to-local conversion.")]
        [SerializeField] private RectTransform canvasRect;

        [Tooltip("Camera rendering the Canvas. Required for Screen Space - Camera.")]
        [SerializeField] private Camera uiCamera;

        private SlotUI sourceCell;
        private InventoryGridUI sourceGrid;
        private int sourceIndex;
        private bool isDragging;

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
                {
                    cell.OnDragStart += HandleDragStart;
                }
            }
        }

        private void OnDisable()
        {
            foreach (InventoryGridUI grid in grids)
            {
                foreach (SlotUI cell in grid.GetComponentsInChildren<SlotUI>(true))
                {
                    cell.OnDragStart -= HandleDragStart;
                }
            }
        }

        private void Update()
        {
            if (!isDragging)
            {
                return;
            }

            UpdateGhostPosition();

            if (PointerReleased())
            {
                CompleteDrag();
            }
        }

        private static bool PointerReleased()
        {
            if (Input.touchCount > 0)
            {
                TouchPhase phase = Input.GetTouch(0).phase;
                return phase == TouchPhase.Ended || phase == TouchPhase.Canceled;
            }

            return Input.GetMouseButtonUp(0);
        }

        private static Vector2 GetCurrentPointerPosition()
        {
            if (Input.touchCount > 0)
            {
                return Input.GetTouch(0).position;
            }

            return Input.mousePosition;
        }

        /// <summary>
        /// Begins a drag from the given cell — caches the source, shows the
        /// ghost with the item's icon, and moves the ghost to the pointer.
        /// </summary>
        private void HandleDragStart(SlotUI cell)
        {
            if (isDragging || !cell.HasItem)
            {
                return;
            }

            if (!TryFindCellLocation(cell, out InventoryGridUI grid, out int index))
            {
                return;
            }

            sourceCell = cell;
            sourceGrid = grid;
            sourceIndex = index;
            isDragging = true;

            dragGhost.sprite = cell.CurrentStack.ItemData.Icon;
            dragGhost.gameObject.SetActive(true);
            UpdateGhostPosition();
        }

        /// <summary>
        /// Resolves which grid a cell belongs to and what slot index it occupies.
        /// Returns false if the cell is not in any of our grids.
        /// </summary>
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

        private void UpdateGhostPosition()
        {
            Vector2 screenPoint = GetCurrentPointerPosition();

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPoint, uiCamera, out Vector2 localPoint);

            ((RectTransform)dragGhost.transform).anchoredPosition = localPoint;
        }

        /// <summary>
        /// On release: find the cell under the pointer; if it belongs to one of
        /// our grids AND is an unlocked slot, transfer or swap. An invalid drop
        /// (outside any grid, or onto a locked cell) leaves the source untouched.
        /// </summary>
        private void CompleteDrag()
        {
            SlotUI targetCell = RaycastForCell(GetCurrentPointerPosition());

            if (targetCell != null
                && TryFindCellLocation(targetCell, out InventoryGridUI targetGrid, out int targetIndex)
                && IsValidDropTarget(targetGrid, targetIndex))
            {
                InventorySystem.TransferOrSwap(
                    sourceGrid.BoundInventory, sourceIndex,
                    targetGrid.BoundInventory, targetIndex);
            }

            EndDrag();
        }

        /// <summary>
        /// A drop target is valid when the destination inventory exists and the
        /// target slot is within its active slot count (i.e. not a locked cell).
        /// </summary>
        private static bool IsValidDropTarget(InventoryGridUI grid, int index)
        {
            InventorySystem inventory = grid.BoundInventory;
            return inventory != null && index < inventory.SlotCount;
        }

        /// <summary>
        /// Raycasts the UI at the given screen point and returns the first
        /// SlotUI hit, or null if the pointer is not over a cell.
        /// </summary>
        private SlotUI RaycastForCell(Vector2 screenPoint)
        {
            PointerEventData data = new PointerEventData(EventSystem.current)
            {
                position = screenPoint,
            };

            List<RaycastResult> hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, hits);

            foreach (RaycastResult hit in hits)
            {
                SlotUI cell = hit.gameObject.GetComponentInParent<SlotUI>();
                if (cell != null)
                {
                    return cell;
                }
            }

            return null;
        }

        private void EndDrag()
        {
            isDragging = false;
            sourceCell = null;
            sourceGrid = null;
            sourceIndex = -1;
            dragGhost.gameObject.SetActive(false);
        }

        /// <summary>True when a drag is currently in progress.</summary>
        public bool IsDragging => isDragging;
    }
}