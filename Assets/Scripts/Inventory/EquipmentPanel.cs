using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.Items
{
    /// <summary>
    /// Connects equipment CellUI cells with EquipmentSystem. Handles:
    ///   - Displaying equipped items
    ///   - Selection of equipment slots
    ///   - Drag highlight for compatible slots
    ///   - Equip via button or double-click from inventory
    ///   - Unequip via double-click on equipment cell
    /// </summary>
    public sealed class EquipmentPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private InventorySelection selection;
        [SerializeField] private ItemActionPanel actionPanel;
        [SerializeField] private InventoryDragController dragController;

        [Header("Equipment Cells")]
        [SerializeField] private CellUI weaponCell;
        [SerializeField] private CellUI backpackCell;
        [SerializeField] private CellUI headCell;
        [SerializeField] private CellUI bodyCell;
        [SerializeField] private CellUI legCell;
        [SerializeField] private CellUI bootsCell;
        [SerializeField] private CellUI quickSlotCell1;
        [SerializeField] private CellUI quickSlotCell2;

        private EquipmentSystem _equipmentSystem;
        private List<CellUI> _allCells;
        private CellUI _selectedEquipCell;

        public event Action<CellUI> OnEquipSelectionChanged;

        public CellUI SelectedEquipCell => _selectedEquipCell;

        // ── Unity lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            _equipmentSystem = new EquipmentSystem();

            _allCells = new List<CellUI>
            {
                weaponCell, backpackCell, headCell, bodyCell,
                legCell, bootsCell, quickSlotCell1, quickSlotCell2
            };
        }

        private void OnEnable()
        {
            foreach (CellUI cell in _allCells)
            {
                if (cell == null) continue;
                cell.OnClicked += HandleCellClicked;
                cell.OnDoubleClicked += HandleCellDoubleClicked;
            }

            selection.OnSelectionChanged += HandleInventorySelectionChanged;
            selection.OnCellDoubleClicked += HandleInventoryDoubleClicked;
            actionPanel.OnEquipRequested += HandleEquipRequested;
            _equipmentSystem.OnSlotChanged += HandleSlotChanged;

            if (dragController != null)
            {
                dragController.OnDragBegan += HandleDragBegan;
                dragController.OnDragEnded += HandleDragEnded;
            }
        }

        private void OnDisable()
        {
            foreach (CellUI cell in _allCells)
            {
                if (cell == null) continue;
                cell.OnClicked -= HandleCellClicked;
                cell.OnDoubleClicked -= HandleCellDoubleClicked;
            }

            selection.OnSelectionChanged -= HandleInventorySelectionChanged;
            selection.OnCellDoubleClicked -= HandleInventoryDoubleClicked;
            actionPanel.OnEquipRequested -= HandleEquipRequested;
            _equipmentSystem.OnSlotChanged -= HandleSlotChanged;

            if (dragController != null)
            {
                dragController.OnDragBegan -= HandleDragBegan;
                dragController.OnDragEnded -= HandleDragEnded;
            }
        }

        // ── Equipment slot selection ─────────────────────────────────────────

        private void HandleCellClicked(CellUI cell)
        {
            if (_selectedEquipCell == cell)
            {
                ClearEquipSelection();
                return;
            }

            selection.Deselect();

            _selectedEquipCell?.SetSelected(false);
            _selectedEquipCell = cell;
            _selectedEquipCell.SetSelected(true);
            OnEquipSelectionChanged?.Invoke(_selectedEquipCell);
        }

        private void HandleCellDoubleClicked(CellUI cell)
        {
            if (!cell.HasItem) return;

            int slotIndex = GetSlotIndex(cell);
            bool unequipped = _equipmentSystem.TryUnequip(
                cell.EquipSlot, slotIndex, playerInventory.Pockets);

            if (!unequipped && playerInventory.Backpack != null)
                _equipmentSystem.TryUnequip(
                    cell.EquipSlot, slotIndex, playerInventory.Backpack);

            ClearEquipSelection();
        }

        private void ClearEquipSelection()
        {
            _selectedEquipCell?.SetSelected(false);
            _selectedEquipCell = null;
            OnEquipSelectionChanged?.Invoke(null);
        }

        // ── Inventory selection changes ──────────────────────────────────────

        private void HandleInventorySelectionChanged(CellUI cell)
        {
            if (cell != null && _selectedEquipCell != null)
                ClearEquipSelection();
        }

        // ── Drag highlights ──────────────────────────────────────────────────

        private void HandleDragBegan(ItemStack stack)
        {
            foreach (CellUI cell in _allCells)
            {
                if (cell == null) continue;
                cell.SetSelected(false);
                cell.SetDragTarget(_equipmentSystem.CanEquipInSlot(stack, cell.EquipSlot));
            }
        }

        private void HandleDragEnded()
        {
            foreach (CellUI cell in _allCells)
            {
                if (cell == null) continue;
                cell.SetDragTarget(false);
            }

            _selectedEquipCell?.SetSelected(true);
        }

        // ── Double-click inventory slot → auto equip ─────────────────────────

        private void HandleInventoryDoubleClicked(CellUI cell)
        {
            if (!cell.HasItem) return;

            InventoryGridUI grid = cell.GetComponentInParent<InventoryGridUI>();
            if (grid == null) return;

            int inventoryIndex = grid.IndexOf(cell);
            if (inventoryIndex < 0) return;

            bool equipped = _equipmentSystem.TryAutoEquip(
                cell.CurrentStack, grid.BoundInventory, inventoryIndex);

            if (equipped)
                selection.Deselect();
        }

        // ── ItemActionPanel equip/unequip requests ───────────────────────────

        private void HandleEquipRequested(ItemStack stack)
        {
            CellUI selectedCell = selection.SelectedCell;
            if (selectedCell == null) return;

            InventoryGridUI grid = selectedCell.GetComponentInParent<InventoryGridUI>();
            if (grid == null) return;

            int inventoryIndex = grid.IndexOf(selectedCell);
            if (inventoryIndex < 0) return;

            _equipmentSystem.TryAutoEquip(stack, grid.BoundInventory, inventoryIndex);
            selection.Deselect();
        }

        public void UnequipSelected()
        {
            if (_selectedEquipCell == null || !_selectedEquipCell.HasItem) return;

            int slotIndex = GetSlotIndex(_selectedEquipCell);
            bool unequipped = _equipmentSystem.TryUnequip(
                _selectedEquipCell.EquipSlot, slotIndex, playerInventory.Pockets);

            if (!unequipped && playerInventory.Backpack != null)
                _equipmentSystem.TryUnequip(
                    _selectedEquipCell.EquipSlot, slotIndex, playerInventory.Backpack);

            ClearEquipSelection();
        }

        // ── Called by InventoryDragController ────────────────────────────────

        public void HandleEquipDropToInventory(CellUI sourceCell,
            InventorySystem targetInventory, int targetIndex)
        {
            int slotIndex = GetSlotIndex(sourceCell);
            ItemStack equipped = _equipmentSystem.GetSlot(sourceCell.EquipSlot, slotIndex);
            if (equipped == null) return;

            ItemStack existing = targetInventory.GetSlot(targetIndex);

            if (existing != null && !_equipmentSystem.CanEquipInSlot(existing, sourceCell.EquipSlot))
            {
                targetInventory.SetSlot(targetIndex, equipped);
                _equipmentSystem.SetSlotDirect(sourceCell.EquipSlot, slotIndex, null);
                return;
            }

            _equipmentSystem.SetSlotDirect(sourceCell.EquipSlot, slotIndex, existing);
            targetInventory.SetSlot(targetIndex, equipped);
        }

        public void HandleInventoryDropToEquip(CellUI sourceCell,
            InventoryGridUI sourceGrid, int sourceIndex, CellUI targetCell)
        {
            if (!sourceCell.HasItem) return;

            int slotIndex = GetSlotIndex(targetCell);
            _equipmentSystem.TryEquip(
                sourceCell.CurrentStack,
                sourceGrid.BoundInventory,
                sourceIndex,
                targetCell.EquipSlot,
                slotIndex);
        }

        public void HandleEquipSwap(CellUI fromCell, CellUI toCell)
        {
            int fromIndex = GetSlotIndex(fromCell);
            int toIndex = GetSlotIndex(toCell);

            ItemStack fromStack = _equipmentSystem.GetSlot(fromCell.EquipSlot, fromIndex);
            ItemStack toStack = _equipmentSystem.GetSlot(toCell.EquipSlot, toIndex);

            if (fromStack != null && !_equipmentSystem.CanEquipInSlot(fromStack, toCell.EquipSlot))
                return;
            if (toStack != null && !_equipmentSystem.CanEquipInSlot(toStack, fromCell.EquipSlot))
                return;

            _equipmentSystem.SetSlotDirect(fromCell.EquipSlot, fromIndex, toStack);
            _equipmentSystem.SetSlotDirect(toCell.EquipSlot, toIndex, fromStack);
        }

        // ── EquipmentSystem → cell visuals ───────────────────────────────────

        private void HandleSlotChanged(EquipSlot slot, int slotIndex, ItemStack stack)
        {
            CellUI cell = GetCell(slot, slotIndex);
            if (cell != null)
                cell.SetStack(stack);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private CellUI GetCell(EquipSlot slot, int slotIndex)
        {
            return slot switch
            {
                EquipSlot.Weapon => weaponCell,
                EquipSlot.Backpack => backpackCell,
                EquipSlot.Helmet => headCell,
                EquipSlot.Jacket => bodyCell,
                EquipSlot.Pants => legCell,
                EquipSlot.Boots => bootsCell,
                EquipSlot.QuickSlot => slotIndex == 0 ? quickSlotCell1 : quickSlotCell2,
                _ => null
            };
        }

        private int GetSlotIndex(CellUI cell)
        {
            return cell == quickSlotCell2 ? 1 : 0;
        }
    }
}