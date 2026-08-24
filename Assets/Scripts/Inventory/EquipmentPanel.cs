using System;
using System.Collections.Generic;
using UnityEngine;
using SimpleSurvival.Player;

namespace SimpleSurvival.Items
{
    public sealed class EquipmentPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerEquipment playerEquipment;
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

        private void Awake()
        {
            _equipmentSystem = playerEquipment.System;

            _allCells = new List<CellUI>
            {
                weaponCell, backpackCell, headCell, bodyCell,
                legCell, bootsCell, quickSlotCell1, quickSlotCell2
            };

            _equipmentSystem.OnSlotChanged += HandleSlotChanged;
        }

        private void OnDestroy()
        {
            if (_equipmentSystem != null)
                _equipmentSystem.OnSlotChanged -= HandleSlotChanged;
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

            if (dragController != null)
            {
                dragController.OnDragBegan += HandleDragBegan;
                dragController.OnDragEnded += HandleDragEnded;
            }

            RefreshAllCells();
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

            if (dragController != null)
            {
                dragController.OnDragBegan -= HandleDragBegan;
                dragController.OnDragEnded -= HandleDragEnded;
            }
        }

        private void RefreshAllCells()
        {
            if (_equipmentSystem == null) return;
            foreach (EquipSlot slot in _equipmentSystem.Slots)
            {
                for (int i = 0; i < _equipmentSystem.SlotCount(slot); i++)
                {
                    CellUI cell = GetCell(slot, i);
                    if (cell != null)
                        cell.SetStack(_equipmentSystem.GetSlot(slot, i));
                }
            }
        }

        private void ExecuteOrQueueIfWeapon(EquipSlot slot, Action action)
        {
            if (action == null) return;

            if (slot == EquipSlot.Weapon && PlayerActionController.Instance != null)
                PlayerActionController.Instance.RequestWeaponSlotAction(action);
            else
                action.Invoke();
        }

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
            if (IsBackpackOccupied(cell)) return;

            int slotIndex = GetSlotIndex(cell);
            EquipSlot slot = cell.EquipSlot;

            ExecuteOrQueueIfWeapon(slot, () =>
            {
                bool unequipped = _equipmentSystem.TryUnequip(slot, slotIndex, playerInventory.Pockets);
                if (!unequipped && playerInventory.Backpack != null)
                    _equipmentSystem.TryUnequip(slot, slotIndex, playerInventory.Backpack);
            });

            ClearEquipSelection();
        }

        private void ClearEquipSelection()
        {
            _selectedEquipCell?.SetSelected(false);
            _selectedEquipCell = null;
            OnEquipSelectionChanged?.Invoke(null);
        }

        private void HandleInventorySelectionChanged(CellUI cell)
        {
            if (cell != null && _selectedEquipCell != null)
                ClearEquipSelection();
        }

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

        private void HandleInventoryDoubleClicked(CellUI cell)
        {
            if (!cell.HasItem) return;
            if (WouldReplaceOccupiedBackpack(cell.CurrentStack)) return;

            InventoryGridUI grid = cell.GetComponentInParent<InventoryGridUI>();
            if (grid == null) return;

            int inventoryIndex = grid.IndexOf(cell);
            if (inventoryIndex < 0) return;

            EquipSlot? targetSlot = _equipmentSystem.GetAutoEquipSlot(cell.CurrentStack);
            ItemStack stackToEquip = cell.CurrentStack;
            InventorySystem boundInventory = grid.BoundInventory;

            void ApplyEquip()
            {
                bool equipped = _equipmentSystem.TryAutoEquip(stackToEquip, boundInventory, inventoryIndex);
                if (equipped)
                    selection.Deselect();
            }

            ExecuteOrQueueIfWeapon(targetSlot ?? EquipSlot.None, ApplyEquip);
        }

        public void UnequipSelected()
        {
            if (_selectedEquipCell == null || !_selectedEquipCell.HasItem) return;
            if (IsBackpackOccupied(_selectedEquipCell)) return;

            int slotIndex = GetSlotIndex(_selectedEquipCell);
            EquipSlot slot = _selectedEquipCell.EquipSlot;

            ExecuteOrQueueIfWeapon(slot, () =>
            {
                bool unequipped = _equipmentSystem.TryUnequip(slot, slotIndex, playerInventory.Pockets);
                if (!unequipped && playerInventory.Backpack != null)
                    _equipmentSystem.TryUnequip(slot, slotIndex, playerInventory.Backpack);
            });

            ClearEquipSelection();
        }

        public void HandleEquipDropToInventory(CellUI sourceCell,
     InventorySystem targetInventory, int targetIndex)
        {
            if (IsBackpackOccupied(sourceCell)) return;
            if (IsDroppingBackpackIntoItself(sourceCell, targetInventory)) return;

            int slotIndex = GetSlotIndex(sourceCell);
            EquipSlot slot = sourceCell.EquipSlot;
            ItemStack equipped = _equipmentSystem.GetSlot(slot, slotIndex);
            if (equipped == null) return;

            ItemStack existing = targetInventory.GetSlot(targetIndex);

            if (existing != null && !_equipmentSystem.CanEquipInSlot(existing, slot))
                return;

            ExecuteOrQueueIfWeapon(slot, () =>
            {
                _equipmentSystem.SetSlotDirect(slot, slotIndex, existing);
                targetInventory.SetSlot(targetIndex, equipped);
            });
        }

        private bool IsDroppingBackpackIntoItself(CellUI sourceCell, InventorySystem targetInventory)
        {
            return sourceCell.EquipSlot == EquipSlot.Backpack
                && targetInventory == playerInventory.Backpack;
        }

        public void HandleInventoryDropToEquip(CellUI sourceCell,
            InventoryGridUI sourceGrid, int sourceIndex, CellUI targetCell)
        {
            if (!sourceCell.HasItem) return;
            if (IsBackpackOccupied(targetCell)) return;
            if (targetCell.EquipSlot == EquipSlot.Backpack
                && _equipmentSystem.CanEquipInSlot(sourceCell.CurrentStack, EquipSlot.Backpack)
                && playerInventory.IsBackpackOccupied()) return;

            int slotIndex = GetSlotIndex(targetCell);
            EquipSlot slot = targetCell.EquipSlot;
            ItemStack stackToEquip = sourceCell.CurrentStack;
            InventorySystem boundInventory = sourceGrid.BoundInventory;

            ExecuteOrQueueIfWeapon(slot, () =>
            {
                _equipmentSystem.TryEquip(
                    stackToEquip,
                    boundInventory,
                    sourceIndex,
                    slot,
                    slotIndex);
            });
        }

        public void HandleEquipSwap(CellUI fromCell, CellUI toCell)
        {
            int fromIndex = GetSlotIndex(fromCell);
            int toIndex = GetSlotIndex(toCell);
            EquipSlot fromSlot = fromCell.EquipSlot;
            EquipSlot toSlot = toCell.EquipSlot;

            ItemStack fromStack = _equipmentSystem.GetSlot(fromSlot, fromIndex);
            ItemStack toStack = _equipmentSystem.GetSlot(toSlot, toIndex);

            if (fromStack != null && !_equipmentSystem.CanEquipInSlot(fromStack, toSlot))
                return;
            if (toStack != null && !_equipmentSystem.CanEquipInSlot(toStack, fromSlot))
                return;

            bool involvesWeapon = fromSlot == EquipSlot.Weapon || toSlot == EquipSlot.Weapon;

            void ApplySwap()
            {
                _equipmentSystem.SetSlotDirect(fromSlot, fromIndex, toStack);
                _equipmentSystem.SetSlotDirect(toSlot, toIndex, fromStack);
            }

            if (involvesWeapon && PlayerActionController.Instance != null)
                PlayerActionController.Instance.RequestWeaponSlotAction(ApplySwap);
            else
                ApplySwap();
        }

        private void HandleSlotChanged(EquipSlot slot, int slotIndex, ItemStack stack)
        {
            CellUI cell = GetCell(slot, slotIndex);
            if (cell != null)
                cell.SetStack(stack);

            if (slot == EquipSlot.Backpack)
                ApplyBackpackResize(stack);
        }

        private void ApplyBackpackResize(ItemStack backpackStack)
        {
            if (backpackStack == null)
            {
                playerInventory.ResizeBackpack(0);
                return;
            }

            ContainerAbility container = backpackStack.ItemData.GetAbility<ContainerAbility>();
            if (container != null)
                playerInventory.ResizeBackpack(container.ExtraSlots);
        }

        private bool IsBackpackOccupied(CellUI cell)
        {
            return cell.EquipSlot == EquipSlot.Backpack && playerInventory.IsBackpackOccupied();
        }

        private bool WouldReplaceOccupiedBackpack(ItemStack stack)
        {
            return _equipmentSystem.GetAutoEquipSlot(stack) == EquipSlot.Backpack
                && playerInventory.IsBackpackOccupied();
        }

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