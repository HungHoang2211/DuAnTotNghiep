using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SimpleSurvival.Items
{
    /// <summary>
    /// Quản lý drag ghost và xử lý tất cả kịch bản drag-drop:
    ///   - Inventory  → Inventory  : TransferOrSwap
    ///   - Inventory  → EquipSlot  : Equip item
    ///   - EquipSlot  → Inventory  : Unequip về đúng slot
    ///   - EquipSlot  → EquipSlot  : Swap equipment
    /// </summary>
    public sealed class InventoryDragController : MonoBehaviour
    {
        [Header("Grids")]
        [SerializeField] private List<InventoryGridUI> grids = new List<InventoryGridUI>();

        [Header("Equipment")]
        [SerializeField] private List<EquipSlotUI> equipCells = new List<EquipSlotUI>();
        [SerializeField] private EquipmentPanel equipmentPanel;

        [Header("Drag Ghost")]
        [SerializeField] private Image dragGhost;
        [SerializeField] private RectTransform canvasRect;
        [SerializeField] private Camera uiCamera;

        // Drag source — chỉ một trong hai có giá trị tại một thời điểm.
        private SlotUI sourceInventoryCell;
        private InventoryGridUI sourceGrid;
        private int sourceIndex;
        private EquipSlotUI sourceEquipCell;

        public bool IsDragging => sourceInventoryCell != null || sourceEquipCell != null;

        /// <summary>Raised when drag starts. Arg: the stack being dragged.</summary>
        public event System.Action<ItemStack> OnDragBegan;
        /// <summary>Raised when drag ends (drop or cancel).</summary>
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
                foreach (SlotUI cell in grid.GetComponentsInChildren<SlotUI>(true))
                    SubscribeInventoryCell(cell);

            foreach (EquipSlotUI cell in equipCells)
                SubscribeEquipCell(cell);
        }

        private void OnDisable()
        {
            foreach (InventoryGridUI grid in grids)
                foreach (SlotUI cell in grid.GetComponentsInChildren<SlotUI>(true))
                    UnsubscribeInventoryCell(cell);

            foreach (EquipSlotUI cell in equipCells)
                UnsubscribeEquipCell(cell);
        }

        // ── Subscription ─────────────────────────────────────────────────────

        private void SubscribeInventoryCell(SlotUI cell)
        {
            cell.OnBeginDragEvent += HandleInventoryBeginDrag;
            cell.OnDragEvent += HandleDrag;
            cell.OnEndDragEvent += HandleEndDrag;
            cell.OnDropEvent += HandleDropOnInventory;
        }

        private void UnsubscribeInventoryCell(SlotUI cell)
        {
            cell.OnBeginDragEvent -= HandleInventoryBeginDrag;
            cell.OnDragEvent -= HandleDrag;
            cell.OnEndDragEvent -= HandleEndDrag;
            cell.OnDropEvent -= HandleDropOnInventory;
        }

        private void SubscribeEquipCell(EquipSlotUI cell)
        {
            cell.OnBeginDragEvent += HandleEquipBeginDrag;
            cell.OnDragEvent += HandleEquipDrag;
            cell.OnEndDragEvent += HandleEquipEndDrag;
            cell.OnDropEvent += HandleDropOnEquip;
        }

        private void UnsubscribeEquipCell(EquipSlotUI cell)
        {
            cell.OnBeginDragEvent -= HandleEquipBeginDrag;
            cell.OnDragEvent -= HandleEquipDrag;
            cell.OnEndDragEvent -= HandleEquipEndDrag;
            cell.OnDropEvent -= HandleDropOnEquip;
        }

        // ── Inventory drag handlers ──────────────────────────────────────────

        private void HandleInventoryBeginDrag(SlotUI cell, PointerEventData eventData)
        {
            if (!TryFindCellLocation(cell, out InventoryGridUI grid, out int index))
                return;

            sourceInventoryCell = cell;
            sourceGrid = grid;
            sourceIndex = index;

            ShowGhost(cell.CurrentStack.ItemData.Icon, eventData.position);
            OnDragBegan?.Invoke(cell.CurrentStack);
        }

        private void HandleDrag(SlotUI cell, PointerEventData eventData)
        {
            if (IsDragging) MoveGhost(eventData.position);
        }

        private void HandleEndDrag(SlotUI cell, PointerEventData eventData)
        {
            EndDrag();
        }

        /// <summary>Inventory → Inventory drop.</summary>
        private void HandleDropOnInventory(SlotUI targetCell)
        {
            if (!IsDragging) return;

            if (sourceInventoryCell != null)
            {
                // Inventory → Inventory
                if (TryFindCellLocation(targetCell, out InventoryGridUI targetGrid, out int targetIndex))
                {
                    InventorySystem.TransferOrSwap(
                        sourceGrid.BoundInventory, sourceIndex,
                        targetGrid.BoundInventory, targetIndex);
                }
            }
            else if (sourceEquipCell != null)
            {
                // EquipSlot → Inventory: unequip vào đúng slot này
                if (TryFindCellLocation(targetCell, out InventoryGridUI targetGrid, out int targetIndex))
                {
                    equipmentPanel.HandleEquipDropToInventory(
                        sourceEquipCell, targetGrid.BoundInventory, targetIndex);
                }
            }

            EndDrag();
        }

        // ── Equipment drag handlers ──────────────────────────────────────────

        private void HandleEquipBeginDrag(EquipSlotUI cell, PointerEventData eventData)
        {
            if (!cell.HasItem) return;

            sourceEquipCell = cell;
            ShowGhost(cell.EquippedStack.ItemData.Icon, eventData.position);
            OnDragBegan?.Invoke(cell.EquippedStack);
        }

        private void HandleEquipDrag(EquipSlotUI cell, PointerEventData eventData)
        {
            if (IsDragging) MoveGhost(eventData.position);
        }

        private void HandleEquipEndDrag(EquipSlotUI cell, PointerEventData eventData)
        {
            EndDrag();
        }

        /// <summary>Inventory hoặc EquipSlot → EquipSlot drop.</summary>
        private void HandleDropOnEquip(EquipSlotUI targetCell)
        {
            if (!IsDragging) return;

            if (sourceInventoryCell != null)
            {
                // Inventory → EquipSlot: equip item
                equipmentPanel.HandleInventoryDropToEquip(
                    sourceInventoryCell, sourceGrid, sourceIndex, targetCell);
            }
            else if (sourceEquipCell != null && sourceEquipCell != targetCell)
            {
                // EquipSlot → EquipSlot: swap
                equipmentPanel.HandleEquipSwap(sourceEquipCell, targetCell);
            }

            EndDrag();
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
            sourceInventoryCell = null;
            sourceGrid = null;
            sourceIndex = -1;
            sourceEquipCell = null;
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