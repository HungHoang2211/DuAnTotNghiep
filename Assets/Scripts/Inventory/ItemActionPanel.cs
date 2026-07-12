using SimpleSurvival.Audio;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleSurvival.Items
{
    public sealed class ItemActionPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InventorySelection inventorySelection;
        [SerializeField] private InventorySelection lootSelection;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private PlayerEquipment playerEquipment;
        [SerializeField] private SimpleSurvival.Player.PlayerConsumableHandler consumableHandler;
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

        public event Action<ItemStack> OnEquipRequested;

        private enum Context
        {
            None,
            Inventory,
            Equipment,
            Loot
        }

        private Context _activeContext = Context.None;
        private CellUI _selectedEquipCell;

        private void Start()
        {
            playerInventory.Pockets.OnInventoryChanged += RefreshAllButtons;
            playerInventory.Pockets.OnInventoryChanged += RefreshSortButton;

            RefreshAllButtons();
            RefreshSortButton();
        }

        private void OnEnable()
        {
            inventorySelection.OnSelectionChanged += HandleInventorySelectionChanged;
            equipmentPanel.OnEquipSelectionChanged += HandleEquipSelectionChanged;

            if (lootSelection != null)
                lootSelection.OnSelectionChanged += HandleLootSelectionChanged;

            buttonUse.onClick.AddListener(HandleUse);
            buttonSplit.onClick.AddListener(HandleSplit);
            buttonSort.onClick.AddListener(HandleSort);
            buttonDelete.onClick.AddListener(HandleDelete);
        }

        private void OnDisable()
        {
            inventorySelection.OnSelectionChanged -= HandleInventorySelectionChanged;
            equipmentPanel.OnEquipSelectionChanged -= HandleEquipSelectionChanged;

            if (lootSelection != null)
                lootSelection.OnSelectionChanged -= HandleLootSelectionChanged;

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

        private void HandleInventorySelectionChanged(CellUI cell)
        {
            if (cell != null)
            {
                _activeContext = Context.Inventory;
                _selectedEquipCell = null;
                if (lootSelection != null) lootSelection.Deselect();
            }
            else if (_activeContext == Context.Inventory)
            {
                _activeContext = Context.None;
            }
            RefreshAllButtons();
        }

        private void HandleEquipSelectionChanged(CellUI cell)
        {
            if (cell != null)
            {
                _activeContext = Context.Equipment;
                _selectedEquipCell = cell;
                inventorySelection.Deselect();
                if (lootSelection != null) lootSelection.Deselect();
            }
            else if (_activeContext == Context.Equipment)
            {
                _activeContext = Context.None;
                _selectedEquipCell = null;
            }
            RefreshAllButtons();
        }

        private void HandleLootSelectionChanged(CellUI cell)
        {
            if (cell != null)
            {
                _activeContext = Context.Loot;
                _selectedEquipCell = null;
                inventorySelection.Deselect();
            }
            else if (_activeContext == Context.Loot)
            {
                _activeContext = Context.None;
            }
            RefreshAllButtons();
        }

        private CellUI GetActiveCell()
        {
            return _activeContext switch
            {
                Context.Inventory => inventorySelection.SelectedCell,
                Context.Equipment => _selectedEquipCell,
                Context.Loot => lootSelection != null ? lootSelection.SelectedCell : null,
                _ => null
            };
        }

        private InventorySelection GetActiveSelection()
        {
            return _activeContext switch
            {
                Context.Inventory => inventorySelection,
                Context.Loot => lootSelection,
                _ => null
            };
        }

        private void RefreshAllButtons()
        {
            switch (_activeContext)
            {
                case Context.Equipment:
                    RefreshForEquipSelection();
                    break;
                case Context.Loot:
                    RefreshForGridSelection(isLoot: true);
                    break;
                case Context.Inventory:
                    RefreshForGridSelection(isLoot: false);
                    break;
                default:
                    RefreshForEmptySelection();
                    break;
            }
        }

        private void RefreshForEmptySelection()
        {
            buttonUse.interactable = false;
            buttonSplit.interactable = false;
            buttonDelete.interactable = false;
            if (useButtonText != null) useButtonText.text = "Use";
        }

        private void RefreshForEquipSelection()
        {
            if (_selectedEquipCell == null)
            {
                RefreshForEmptySelection();
                return;
            }

            bool hasItem = _selectedEquipCell.HasItem;
            buttonUse.interactable = hasItem;
            buttonSplit.interactable = false;
            buttonDelete.interactable = false;

            if (useButtonText != null)
                useButtonText.text = "Unequip";
        }

        private void RefreshForGridSelection(bool isLoot)
        {
            CellUI cell = GetActiveCell();
            bool hasItem = cell != null && cell.HasItem;

            if (!hasItem)
            {
                RefreshForEmptySelection();
                return;
            }

            ItemStack stack = cell.CurrentStack;
            bool isEquippable = playerEquipment.System.IsEquippable(stack);
            bool isConsumable = stack.ItemData.HasAbility<ConsumableAbility>();
            bool blockedByOccupiedBackpack = isEquippable && WouldReplaceOccupiedBackpack(stack);

            buttonUse.interactable = (isConsumable || isEquippable) && !blockedByOccupiedBackpack;

            if (useButtonText != null)
                useButtonText.text = isEquippable ? "Equip" : "Use";

            InventoryGridUI grid = cell.GetComponentInParent<InventoryGridUI>();
            bool hasFreeSlot = grid != null
                && HasFreeSlotInGrid(grid.BoundInventory, grid.IndexOf(cell));

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

        private void HandleUse()
        {
            if (_activeContext == Context.Equipment)
            {
                if (_selectedEquipCell != null && _selectedEquipCell.HasItem)
                {
                    equipmentPanel.UnequipSelected();
                    PlayEquipSound();
                }
                return;
            }

            CellUI cell = GetActiveCell();
            if (cell == null || !cell.HasItem) return;

            ItemStack stack = cell.CurrentStack;

            ConsumableAbility consumable = stack.ItemData.GetAbility<ConsumableAbility>();
            if (consumable != null)
            {
                if (consumableHandler != null && consumableHandler.TryConsume(stack))
                    ConsumeOne(cell);
                return;
            }

            if (playerEquipment.System.IsEquippable(stack))
            {
                EquipFromActiveCell(cell, stack);
            }
        }
        private void EquipFromActiveCell(CellUI cell, ItemStack stack)
        {
            if (WouldReplaceOccupiedBackpack(stack)) return;

            InventoryGridUI grid = cell.GetComponentInParent<InventoryGridUI>();
            if (grid == null) return;

            int index = grid.IndexOf(cell);
            if (index < 0) return;

            bool equipped = playerEquipment.System.TryAutoEquip(
                stack, grid.BoundInventory, index);

            if (equipped)
            {
                InventorySelection sel = GetActiveSelection();
                if (sel != null) sel.Deselect();

                PlayEquipSound();
            }
        }

        private bool WouldReplaceOccupiedBackpack(ItemStack stack)
        {
            return playerEquipment.System.GetAutoEquipSlot(stack) == EquipSlot.Backpack
                && playerInventory.IsBackpackOccupied();
        }
        private void PlayEquipSound()
        {
            if (UIAudioController.Instance != null)
                UIAudioController.Instance.PlayClick();
        }
        private void PlaydeleteSound()
        {
            if (UIAudioController.Instance != null)
                UIAudioController.Instance.PlayClick();
        }

        private void HandleSplit()
        {
            CellUI cell = GetActiveCell();
            if (cell == null || !cell.HasItem) return;

            InventoryGridUI grid = cell.GetComponentInParent<InventoryGridUI>();
            if (grid == null) return;

            int index = grid.IndexOf(cell);
            if (index < 0) return;

            InventorySystem inventory = grid.BoundInventory;
            ItemStack stack = inventory.GetSlot(index);

            if (stack == null || !stack.ItemData.IsStackable || stack.Quantity < 2)
                return;

            int emptyIndex = FindEmptySlotExcluding(inventory, index);
            if (emptyIndex < 0) return;

            int splitAmount = stack.Quantity / 2;
            stack.RemoveQuantity(splitAmount);
            inventory.SetSlot(emptyIndex, new ItemStack(stack.ItemData, splitAmount));

            PlayEquipSound();

            InventorySelection sel = GetActiveSelection();
            if (stack.Quantity < 2 && sel != null)
                sel.Deselect();
            else
                inventory.NotifyChanged();
        }

        private void HandleSort()
        {
            if (playerInventory.Backpack != null)
                InventorySystem.SortTogether(playerInventory.Pockets, playerInventory.Backpack);
            else
                playerInventory.Pockets.Sort();

            inventorySelection.Deselect();
        }

        private void PlayDeleteSound()
        {
            if (UIAudioController.Instance != null)
                UIAudioController.Instance.PlayDelete();
        }

        private void HandleDelete()
        {
            CellUI cell = GetActiveCell();
            if (cell == null || !cell.HasItem) return;

            InventoryGridUI grid = cell.GetComponentInParent<InventoryGridUI>();
            if (grid == null) return;

            int index = grid.IndexOf(cell);
            if (index < 0) return;

            ItemStack stack = grid.BoundInventory.GetSlot(index);
            if (stack == null) return;

            InventorySystem inventory = grid.BoundInventory;
            InventorySelection sel = GetActiveSelection();
            PlayEquipSound();

            confirmDeleteDialog.Show(
                $"Delete {stack.ItemData.ItemName}?",
                confirmed =>
                {
                    if (!confirmed) return;
                    inventory.SetSlot(index, null);
                    if (sel != null) sel.Deselect();

                    PlayDeleteSound();
                });
        }
        private void ConsumeOne(CellUI cell)
        {
            InventoryGridUI grid = cell.GetComponentInParent<InventoryGridUI>();
            if (grid == null) return;

            int index = grid.IndexOf(cell);
            if (index < 0) return;

            InventorySystem inventory = grid.BoundInventory;
            ItemStack stack = inventory.GetSlot(index);
            if (stack == null) return;

            stack.RemoveQuantity(1);

            if (stack.IsEmpty)
            {
                inventory.SetSlot(index, null);
                InventorySelection sel = GetActiveSelection();
                if (sel != null) sel.Deselect();
            }
            else
            {
                inventory.NotifyChanged();
            }
        }

        private bool HasFreeSlotInGrid(InventorySystem inventory, int excludeIndex)
        {
            return FindEmptySlotExcluding(inventory, excludeIndex) >= 0;
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