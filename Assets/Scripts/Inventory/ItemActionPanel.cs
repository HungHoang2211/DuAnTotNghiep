using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleSurvival.Items
{
    public sealed class ItemActionPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InventorySelection selection;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private EquipmentPanel equipmentPanel;

        [Header("Buttons")]
        [SerializeField] private Button buttonUse;
        [SerializeField] private Button buttonSplit;
        [SerializeField] private Button buttonSort;
        [SerializeField] private Button buttonDelete;

        [Header("Use Button Text")]
        [SerializeField] private TMP_Text useButtonText;

        [Header("Dialogs")]
        [SerializeField] private SimpleSurvival.UI.ConfirmDeleteDialog confirmDeleteDialog;

        public event Action<ItemStack> OnUseConsumableRequested;
        public event Action<ItemStack> OnEquipRequested;

        private CellUI _selectedEquipCell;

        // ── Unity lifecycle ──────────────────────────────────────────────────

        private void Start()
        {
            playerInventory.Pockets.OnInventoryChanged += RefreshAllButtons;
            playerInventory.Pockets.OnInventoryChanged += RefreshSortButton;

            RefreshAllButtons();
            RefreshSortButton();
        }

        private void OnEnable()
        {
            selection.OnSelectionChanged += HandleInventorySelectionChanged;
            equipmentPanel.OnEquipSelectionChanged += HandleEquipSelectionChanged;

            buttonUse.onClick.AddListener(HandleUse);
            buttonSplit.onClick.AddListener(HandleSplit);
            buttonSort.onClick.AddListener(HandleSort);
            buttonDelete.onClick.AddListener(HandleDelete);
        }

        private void OnDisable()
        {
            selection.OnSelectionChanged -= HandleInventorySelectionChanged;
            equipmentPanel.OnEquipSelectionChanged -= HandleEquipSelectionChanged;

            if (playerInventory != null && playerInventory.Pockets != null)
            {
                playerInventory.Pockets.OnInventoryChanged -= RefreshAllButtons;
                playerInventory.Pockets.OnInventoryChanged -= RefreshSortButton;
            }

            buttonUse.onClick.RemoveListener(HandleUse);
            buttonSplit.onClick.RemoveListener(HandleSplit);
            buttonSort.onClick.RemoveListener(HandleSort);
            buttonDelete.onClick.RemoveListener(HandleDelete);
        }

        // ── Selection handlers ───────────────────────────────────────────────

        private void HandleInventorySelectionChanged(CellUI cell)
        {
            _selectedEquipCell = null;
            RefreshAllButtons();
        }

        private void HandleEquipSelectionChanged(CellUI cell)
        {
            _selectedEquipCell = cell;
            RefreshAllButtons();
        }

        // ── Button state refresh ─────────────────────────────────────────────

        private void RefreshAllButtons()
        {
            if (_selectedEquipCell != null)
            {
                RefreshForEquipSelection();
                return;
            }

            RefreshForInventorySelection();
        }

        private void RefreshForEquipSelection()
        {
            bool hasItem = _selectedEquipCell.HasItem;
            buttonUse.interactable = hasItem;
            buttonSplit.interactable = false;
            buttonDelete.interactable = false;

            if (useButtonText != null)
                useButtonText.text = "Unequip";
        }

        private void RefreshForInventorySelection()
        {
            CellUI cell = selection.SelectedCell;
            bool hasItem = cell != null && cell.HasItem;

            if (!hasItem)
            {
                buttonUse.interactable = false;
                buttonSplit.interactable = false;
                buttonDelete.interactable = false;
                if (useButtonText != null) useButtonText.text = "Use";
                return;
            }

            ItemStack stack = cell.CurrentStack;
            bool isEquippable = stack.ItemData.HasAbility<EquipmentAbility>()
                             || stack.ItemData.HasAbility<WeaponAbility>();
            bool isConsumable = stack.ItemData.HasAbility<ConsumableAbility>();

            buttonUse.interactable = isConsumable || isEquippable;

            if (useButtonText != null)
                useButtonText.text = isEquippable ? "Equip" : "Use";

            InventoryGridUI splitGrid = cell.GetComponentInParent<InventoryGridUI>();
            bool hasFreeSlot = splitGrid != null
                && HasFreeSlotForSplit(splitGrid.BoundInventory, splitGrid.IndexOf(cell));

            buttonSplit.interactable = stack.ItemData.IsStackable
                && stack.Quantity > 1
                && hasFreeSlot;

            buttonDelete.interactable = true;
        }

        private void RefreshSortButton()
        {
            bool hasAnyItem = InventoryHasItem(playerInventory.Pockets)
                || (playerInventory.Backpack != null
                    && InventoryHasItem(playerInventory.Backpack));

            buttonSort.interactable = hasAnyItem;
        }

        // ── Button handlers ──────────────────────────────────────────────────

        private void HandleUse()
        {
            if (_selectedEquipCell != null && _selectedEquipCell.HasItem)
            {
                equipmentPanel.UnequipSelected();
                return;
            }

            if (!TryGetSelectedStack(out ItemStack stack))
                return;

            ConsumableAbility consumable = stack.ItemData.GetAbility<ConsumableAbility>();
            if (consumable != null)
            {
                OnUseConsumableRequested?.Invoke(stack);
                ConsumeOne();
                return;
            }

            if (stack.ItemData.HasAbility<EquipmentAbility>()
                || stack.ItemData.HasAbility<WeaponAbility>())
                OnEquipRequested?.Invoke(stack);
        }

        private void HandleSplit()
        {
            if (!TryFindSelectedLocation(out InventoryGridUI grid, out int index))
                return;

            InventorySystem inventory = grid.BoundInventory;
            ItemStack stack = inventory.GetSlot(index);

            if (stack == null || !stack.ItemData.IsStackable || stack.Quantity < 2)
                return;

            int emptyIndex = FindEmptySlotExcluding(inventory, index);
            InventorySystem targetInventory = inventory;

            if (emptyIndex < 0)
            {
                InventorySystem other = GetOtherInventory(inventory);
                if (other != null)
                {
                    emptyIndex = FindEmptySlotExcluding(other, -1);
                    targetInventory = other;
                }
            }

            if (emptyIndex < 0) return;

            int splitAmount = stack.Quantity / 2;
            stack.RemoveQuantity(splitAmount);
            targetInventory.SetSlot(emptyIndex, new ItemStack(stack.ItemData, splitAmount));

            if (stack.Quantity < 2)
                selection.Deselect();
            else
                inventory.NotifyChanged();
        }

        private void HandleSort()
        {
            if (playerInventory.Backpack != null)
                InventorySystem.SortTogether(playerInventory.Pockets, playerInventory.Backpack);
            else
                playerInventory.Pockets.Sort();

            selection.Deselect();
        }

        private void HandleDelete()
        {
            if (!TryFindSelectedLocation(out InventoryGridUI grid, out int index))
                return;

            ItemStack stack = grid.BoundInventory.GetSlot(index);
            if (stack == null) return;

            confirmDeleteDialog.Show(
                $"Delete {stack.ItemData.ItemName}?",
                confirmed =>
                {
                    if (!confirmed) return;
                    if (!TryFindSelectedLocation(out InventoryGridUI g, out int i)) return;
                    g.BoundInventory.SetSlot(i, null);
                    selection.Deselect();
                });
        }

        // ── Private helpers ──────────────────────────────────────────────────

        private void ConsumeOne()
        {
            if (!TryFindSelectedLocation(out InventoryGridUI grid, out int index))
                return;

            InventorySystem inventory = grid.BoundInventory;
            ItemStack stack = inventory.GetSlot(index);
            if (stack == null) return;

            stack.RemoveQuantity(1);

            if (stack.IsEmpty)
                inventory.SetSlot(index, null);
            else
                inventory.NotifyChanged();

            selection.Deselect();
        }

        private bool TryGetSelectedStack(out ItemStack stack)
        {
            CellUI cell = selection.SelectedCell;
            if (cell != null && cell.HasItem)
            {
                stack = cell.CurrentStack;
                return true;
            }
            stack = null;
            return false;
        }

        private bool TryFindSelectedLocation(out InventoryGridUI foundGrid, out int foundIndex)
        {
            CellUI cell = selection.SelectedCell;
            if (cell != null)
            {
                InventoryGridUI grid = cell.GetComponentInParent<InventoryGridUI>();
                if (grid != null)
                {
                    int index = grid.IndexOf(cell);
                    if (index >= 0)
                    {
                        foundGrid = grid;
                        foundIndex = index;
                        return true;
                    }
                }
            }
            foundGrid = null;
            foundIndex = -1;
            return false;
        }

        private bool HasFreeSlotForSplit(InventorySystem inventory, int excludeIndex)
        {
            if (FindEmptySlotExcluding(inventory, excludeIndex) >= 0)
                return true;

            InventorySystem other = GetOtherInventory(inventory);
            return other != null && FindEmptySlotExcluding(other, -1) >= 0;
        }

        private InventorySystem GetOtherInventory(InventorySystem current)
        {
            if (current == playerInventory.Pockets) return playerInventory.Backpack;
            if (current == playerInventory.Backpack) return playerInventory.Pockets;
            return null;
        }

        private static bool InventoryHasItem(InventorySystem inventory)
        {
            for (int i = 0; i < inventory.SlotCount; i++)
                if (inventory.GetSlot(i) != null) return true;
            return false;
        }

        private static int FindEmptySlotExcluding(InventorySystem inventory, int excludeIndex)
        {
            for (int i = 0; i < inventory.SlotCount; i++)
                if (i != excludeIndex && inventory.GetSlot(i) == null) return i;
            return -1;
        }
    }
}