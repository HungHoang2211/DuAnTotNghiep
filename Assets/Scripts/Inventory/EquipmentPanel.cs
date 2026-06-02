using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.Items
{
    /// <summary>
    /// Connects EquipSlotUI cells with EquipmentSystem. Handles:
    ///   - Displaying equipped items in each cell
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
        [SerializeField] private EquipSlotUI weaponCell;
        [SerializeField] private EquipSlotUI backpackCell;
        [SerializeField] private EquipSlotUI headCell;
        [SerializeField] private EquipSlotUI bodyCell;
        [SerializeField] private EquipSlotUI legCell;
        [SerializeField] private EquipSlotUI bootsCell;
        [SerializeField] private EquipSlotUI quickSlotCell1;
        [SerializeField] private EquipSlotUI quickSlotCell2;

        private EquipmentSystem _equipmentSystem;
        private List<EquipSlotUI> _allCells;
        private EquipSlotUI _selectedEquipCell;

        // ── Events ───────────────────────────────────────────────────────────

        /// <summary>Raised when equipment slot selection changes. Null = deselected.</summary>
        public event Action<EquipSlotUI> OnEquipSelectionChanged;

        // ── Properties ───────────────────────────────────────────────────────

        public EquipSlotUI SelectedEquipCell => _selectedEquipCell;

        // ── Unity lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            _equipmentSystem = new EquipmentSystem();

            _allCells = new List<EquipSlotUI>
            {
                weaponCell, backpackCell, headCell, bodyCell,
                legCell, bootsCell, quickSlotCell1, quickSlotCell2
            };
        }

        private void OnEnable()
        {
            foreach (EquipSlotUI cell in _allCells)
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
            foreach (EquipSlotUI cell in _allCells)
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

        private void HandleCellClicked(EquipSlotUI cell)
        {
            if (_selectedEquipCell == cell)
            {
                ClearEquipSelection();
                return;
            }

            // Clear inventory selection when selecting equipment slot.
            selection.Deselect();

            _selectedEquipCell?.SetSelected(false);
            _selectedEquipCell = cell;
            _selectedEquipCell.SetSelected(true);
            OnEquipSelectionChanged?.Invoke(_selectedEquipCell);
        }

        private void HandleCellDoubleClicked(EquipSlotUI cell)
        {
            if (!cell.HasItem) return;

            int slotIndex = GetSlotIndex(cell);
            bool unequipped = _equipmentSystem.TryUnequip(
                cell.EquipSlot, slotIndex, playerInventory.Pockets);

            if (!unequipped && playerInventory.Backpack != null)
                _equipmentSystem.TryUnequip(cell.EquipSlot, slotIndex,
                    playerInventory.Backpack);

            ClearEquipSelection();
        }

        private void ClearEquipSelection()
        {
            _selectedEquipCell?.SetSelected(false);
            _selectedEquipCell = null;
            OnEquipSelectionChanged?.Invoke(null);
        }

        // ── Inventory selection changes ──────────────────────────────────────

        private void HandleInventorySelectionChanged(SlotUI slot)
        {
            // Clear equipment selection when inventory slot is selected.
            if (slot != null && _selectedEquipCell != null)
                ClearEquipSelection();
        }

        // ── Drag highlights ──────────────────────────────────────────────────

        private void HandleDragBegan(ItemStack stack)
        {
            Debug.Log($"HandleDragBegan: {stack.ItemData.ItemName}");
            foreach (EquipSlotUI cell in _allCells)
            {
                if (cell == null) continue;
                cell.SetSelected(false);
                bool compatible = _equipmentSystem.CanEquipInSlot(stack, cell.EquipSlot);
                Debug.Log($"  {cell.name} ({cell.EquipSlot}): compatible={compatible}");
                cell.SetDragTarget(compatible);
            }
        }

        private void HandleDragEnded()
        {
            Debug.Log("HandleDragEnded");
            foreach (EquipSlotUI cell in _allCells)
            {
                if (cell == null) continue;
                cell.SetDragTarget(false);
            }

            // Restore selection highlight if a cell is still selected.
            if (_selectedEquipCell != null)
                _selectedEquipCell.SetSelected(true);
        }

        // ── Double-click inventory slot → auto equip ─────────────────────────

        private void HandleInventoryDoubleClicked(SlotUI slot)
        {
            if (!slot.HasItem) return;

            InventoryGridUI grid = slot.GetComponentInParent<InventoryGridUI>();
            if (grid == null) return;

            int inventoryIndex = grid.IndexOf(slot);
            if (inventoryIndex < 0) return;

            bool equipped = _equipmentSystem.TryAutoEquip(
                slot.CurrentStack, grid.BoundInventory, inventoryIndex);

            if (equipped)
                selection.Deselect();
        }

        // ── ItemActionPanel equip/unequip requests ───────────────────────────

        private void HandleEquipRequested(ItemStack stack)
        {
            SlotUI selectedSlot = selection.SelectedSlot;
            if (selectedSlot == null) return;

            InventoryGridUI grid = selectedSlot.GetComponentInParent<InventoryGridUI>();
            if (grid == null) return;

            int inventoryIndex = grid.IndexOf(selectedSlot);
            if (inventoryIndex < 0) return;

            _equipmentSystem.TryAutoEquip(stack, grid.BoundInventory, inventoryIndex);
            selection.Deselect();
        }

        /// <summary>Called by ItemActionPanel when Unequip button is pressed.</summary>
        public void UnequipSelected()
        {
            if (_selectedEquipCell == null || !_selectedEquipCell.HasItem) return;

            int slotIndex = GetSlotIndex(_selectedEquipCell);
            bool unequipped = _equipmentSystem.TryUnequip(
                _selectedEquipCell.EquipSlot, slotIndex, playerInventory.Pockets);

            if (!unequipped && playerInventory.Backpack != null)
                _equipmentSystem.TryUnequip(_selectedEquipCell.EquipSlot, slotIndex,
                    playerInventory.Backpack);

            ClearEquipSelection();
        }

        // ── Called by InventoryDragController ────────────────────────────────

        public void HandleEquipDropToInventory(EquipSlotUI sourceCell,
            InventorySystem targetInventory, int targetIndex)
        {
            int slotIndex = GetSlotIndex(sourceCell);
            ItemStack equipped = _equipmentSystem.GetSlot(sourceCell.EquipSlot, slotIndex);
            if (equipped == null) return;

            ItemStack existing = targetInventory.GetSlot(targetIndex);
            _equipmentSystem.SetSlotDirect(sourceCell.EquipSlot, slotIndex, existing);
            targetInventory.SetSlot(targetIndex, equipped);
        }

        public void HandleInventoryDropToEquip(SlotUI sourceSlot,
            InventoryGridUI sourceGrid, int sourceIndex, EquipSlotUI targetCell)
        {
            if (!sourceSlot.HasItem) return;

            int slotIndex = GetSlotIndex(targetCell);
            _equipmentSystem.TryEquip(
                sourceSlot.CurrentStack,
                sourceGrid.BoundInventory,
                sourceIndex,
                targetCell.EquipSlot,
                slotIndex);
        }

        public void HandleEquipSwap(EquipSlotUI fromCell, EquipSlotUI toCell)
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
            EquipSlotUI cell = GetCell(slot, slotIndex);
            if (cell != null)
                cell.SetStack(stack);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private EquipSlotUI GetCell(EquipSlot slot, int slotIndex)
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

        private int GetSlotIndex(EquipSlotUI cell)
        {
            return cell == quickSlotCell2 ? 1 : 0;
        }
    }
}