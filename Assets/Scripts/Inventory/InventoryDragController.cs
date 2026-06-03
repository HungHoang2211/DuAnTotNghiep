using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SimpleSurvival.Items
{
    /// <summary>
    /// Manages the drag ghost and handles all drag-drop scenarios:
    ///   - Inventory  → Inventory  : TransferOrSwap
    ///   - Inventory  → EquipSlot  : Equip item
    ///   - EquipSlot  → Inventory  : Unequip to target slot
    ///   - EquipSlot  → EquipSlot  : Swap equipment
    /// </summary>
    public sealed class InventoryDragController : MonoBehaviour
    {
        [Header("Grids")]
        [SerializeField] private List<InventoryGridUI> grids = new List<InventoryGridUI>();

        [Header("Equipment")]
        [SerializeField] private List<CellUI> equipCells = new List<CellUI>();
        [SerializeField] private EquipmentPanel equipmentPanel;

        [Header("Drag Ghost")]
        [SerializeField] private Image dragGhost;
        [SerializeField] private RectTransform canvasRect;
        [SerializeField] private Camera uiCamera;

        private CellUI _sourceInventoryCell;
        private InventoryGridUI _sourceGrid;
        private int _sourceIndex;
        private CellUI _sourceEquipCell;

        private readonly List<CellUI> _subscribedCells = new List<CellUI>();

        public bool IsDragging => _sourceInventoryCell != null || _sourceEquipCell != null;

        public event System.Action<ItemStack> OnDragBegan;
        public event System.Action OnDragEnded;

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
                foreach (CellUI cell in grid.GetComponentsInChildren<CellUI>(true))
                    Subscribe(cell);

            foreach (CellUI cell in equipCells)
                Subscribe(cell);
        }

        private void OnDisable()
        {
            foreach (CellUI cell in _subscribedCells)
                Unsubscribe(cell);

            _subscribedCells.Clear();
        }

        // ── Subscription ─────────────────────────────────────────────────────

        private void Subscribe(CellUI cell)
        {
            cell.OnBeginDragEvent += HandleBeginDrag;
            cell.OnDragEvent += HandleDrag;
            cell.OnEndDragEvent += HandleEndDrag;
            cell.OnDropEvent += HandleDrop;
            _subscribedCells.Add(cell);
        }

        private void Unsubscribe(CellUI cell)
        {
            cell.OnBeginDragEvent -= HandleBeginDrag;
            cell.OnDragEvent -= HandleDrag;
            cell.OnEndDragEvent -= HandleEndDrag;
            cell.OnDropEvent -= HandleDrop;
        }

        // ── Drag handlers ────────────────────────────────────────────────────

        private void HandleBeginDrag(CellUI cell, PointerEventData eventData)
        {
            if (!cell.HasItem) return;

            if (cell.IsEquipCell)
            {
                _sourceEquipCell = cell;
            }
            else
            {
                if (!TryFindCellLocation(cell, out InventoryGridUI grid, out int index))
                    return;

                _sourceInventoryCell = cell;
                _sourceGrid = grid;
                _sourceIndex = index;
            }

            ShowGhost(cell.CurrentStack.ItemData.Icon, eventData.position);
            OnDragBegan?.Invoke(cell.CurrentStack);
        }

        private void HandleDrag(CellUI cell, PointerEventData eventData)
        {
            if (IsDragging) MoveGhost(eventData.position);
        }

        private void HandleEndDrag(CellUI cell, PointerEventData eventData)
        {
            EndDrag();
        }

        private void HandleDrop(CellUI targetCell)
        {
            if (!IsDragging) return;

            if (targetCell.IsEquipCell)
                DropOnEquip(targetCell);
            else
                DropOnInventory(targetCell);

            EndDrag();
        }

        // ── Drop logic ───────────────────────────────────────────────────────

        private void DropOnInventory(CellUI targetCell)
        {
            if (!TryFindCellLocation(targetCell, out InventoryGridUI targetGrid, out int targetIndex))
                return;

            if (_sourceInventoryCell != null)
            {
                InventorySystem.TransferOrSwap(
                    _sourceGrid.BoundInventory, _sourceIndex,
                    targetGrid.BoundInventory, targetIndex);
            }
            else if (_sourceEquipCell != null)
            {
                equipmentPanel.HandleEquipDropToInventory(
                    _sourceEquipCell, targetGrid.BoundInventory, targetIndex);
            }
        }

        private void DropOnEquip(CellUI targetCell)
        {
            if (_sourceInventoryCell != null)
            {
                equipmentPanel.HandleInventoryDropToEquip(
                    _sourceInventoryCell, _sourceGrid, _sourceIndex, targetCell);
            }
            else if (_sourceEquipCell != null && _sourceEquipCell != targetCell)
            {
                equipmentPanel.HandleEquipSwap(_sourceEquipCell, targetCell);
            }
        }

        // ── Ghost helpers ────────────────────────────────────────────────────

        private void ShowGhost(Sprite sprite, Vector2 screenPos)
        {
            dragGhost.sprite = sprite;
            dragGhost.gameObject.SetActive(true);
            MoveGhost(screenPos);
        }

        private void MoveGhost(Vector2 screenPoint)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPoint, uiCamera, out Vector2 localPoint);
            ((RectTransform)dragGhost.transform).anchoredPosition = localPoint;
        }

        private void EndDrag()
        {
            if (!IsDragging) return;

            OnDragEnded?.Invoke();
            _sourceInventoryCell = null;
            _sourceGrid = null;
            _sourceIndex = -1;
            _sourceEquipCell = null;
            dragGhost.gameObject.SetActive(false);
        }

        private bool TryFindCellLocation(CellUI cell, out InventoryGridUI grid, out int index)
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