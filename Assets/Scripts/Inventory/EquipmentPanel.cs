using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SimpleSurvival.Items
{
    /// <summary>
    /// Connects EquipSlotUI cells with EquipmentSystem. Handles:
    ///   - Displaying equipped items in each cell
    ///   - Equip via button Use/Equip in ItemActionPanel
    ///   - Equip via drag-drop onto a cell
    ///   - Double-click a cell to unequip back to inventory
    ///   - Updating Use button text based on selected item type
    /// </summary>
    public sealed class EquipmentPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private InventorySelection selection;
        [SerializeField] private ItemActionPanel actionPanel;

        [Header("Equipment Cells")]
        [SerializeField] private EquipSlotUI weaponCell;
        [SerializeField] private EquipSlotUI backpackCell;
        [SerializeField] private EquipSlotUI headCell;
        [SerializeField] private EquipSlotUI bodyCell;
        [SerializeField] private EquipSlotUI legCell;
        [SerializeField] private EquipSlotUI bootsCell;
        [SerializeField] private EquipSlotUI quickSlotCell1;
        [SerializeField] private EquipSlotUI quickSlotCell2;

        [Header("Use Button Text")]
        [SerializeField] private TMP_Text useButtonText;

        private EquipmentSystem _equipmentSystem;
        private List<EquipSlotUI> _allCells;

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

            selection.OnSelectionChanged += HandleSelectionChanged;
            selection.OnCellDoubleClicked += HandleInventoryDoubleClicked;
            actionPanel.OnEquipRequested += HandleEquipRequested;
            _equipmentSystem.OnSlotChanged += HandleSlotChanged;
        }

        private void OnDisable()
        {
            foreach (EquipSlotUI cell in _allCells)
            {
                if (cell == null) continue;
                cell.OnClicked -= HandleCellClicked;
                cell.OnDoubleClicked -= HandleCellDoubleClicked;

            }

            selection.OnSelectionChanged -= HandleSelectionChanged;
            selection.OnCellDoubleClicked -= HandleInventoryDoubleClicked;
            actionPanel.OnEquipRequested -= HandleEquipRequested;
            _equipmentSystem.OnSlotChanged -= HandleSlotChanged;
        }

        // ── Cell event handlers ──────────────────────────────────────────────

        private void HandleCellClicked(EquipSlotUI cell)
        {
            // Deselect inventory selection when clicking equipment cell.
            selection.Deselect();
        }

        private void HandleCellDoubleClicked(EquipSlotUI cell)
        {
            if (!cell.HasItem)
                return;

            int slotIndex = GetSlotIndex(cell);
            bool unequipped = _equipmentSystem.TryUnequip(
                cell.EquipSlot, slotIndex, playerInventory.Pockets);

            if (!unequipped && playerInventory.Backpack != null)
                _equipmentSystem.TryUnequip(cell.EquipSlot, slotIndex,
                    playerInventory.Backpack);
        }


        // ── Inventory selection → Use button text ────────────────────────────

        private void HandleSelectionChanged(SlotUI slot)
        {
            if (useButtonText == null)
                return;

            if (slot == null || !slot.HasItem)
            {
                useButtonText.text = "Use";
                return;
            }

            ItemData data = slot.CurrentStack.ItemData;

            if (data.HasAbility<EquipmentAbility>() || data.HasAbility<WeaponAbility>())
                useButtonText.text = "Equip";
            else
                useButtonText.text = "Use";
        }

        // ── Double-click inventory slot → auto equip ────────────────────────

        private void HandleInventoryDoubleClicked(SlotUI slot)
        {
            if (!slot.HasItem)
                return;

            InventoryGridUI grid = slot.GetComponentInParent<InventoryGridUI>();
            if (grid == null)
                return;

            int inventoryIndex = grid.IndexOf(slot);
            if (inventoryIndex < 0)
                return;

            bool equipped = _equipmentSystem.TryAutoEquip(
                slot.CurrentStack, grid.BoundInventory, inventoryIndex);

            if (equipped)
                selection.Deselect();
        }

        // ── ItemActionPanel equip request (Use/Equip button) ─────────────────

        private void HandleEquipRequested(ItemStack stack)
        {
            SlotUI selectedSlot = selection.SelectedSlot;
            if (selectedSlot == null)
                return;

            InventoryGridUI grid = selectedSlot.GetComponentInParent<InventoryGridUI>();
            if (grid == null)
                return;

            int inventoryIndex = grid.IndexOf(selectedSlot);
            if (inventoryIndex < 0)
                return;

            _equipmentSystem.TryAutoEquip(stack, grid.BoundInventory, inventoryIndex);
            selection.Deselect();
        }

        // ── Called by InventoryDragController ───────────────────────────────────

        /// <summary>EquipSlot → Inventory: unequip vào đúng slot inventory.</summary>
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

        /// <summary>Inventory → EquipSlot: equip item từ inventory.</summary>
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

        /// <summary>EquipSlot → EquipSlot: swap hai item trang bị.</summary>
        public void HandleEquipSwap(EquipSlotUI fromCell, EquipSlotUI toCell)
        {
            int fromIndex = GetSlotIndex(fromCell);
            int toIndex = GetSlotIndex(toCell);

            ItemStack fromStack = _equipmentSystem.GetSlot(fromCell.EquipSlot, fromIndex);
            ItemStack toStack = _equipmentSystem.GetSlot(toCell.EquipSlot, toIndex);

            // Check compatibility before swapping.
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
            if (cell == quickSlotCell2)
                return 1;
            return 0;
        }
    }
}