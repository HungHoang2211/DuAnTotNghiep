using System;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleSurvival.Items
{
    /// <summary>
    /// Drives the four action buttons (Use, Split, Sort, Delete) for whichever
    /// inventory slot is currently selected. The panel is always visible —
    /// buttons are greyed out when their action is not applicable.
    ///
    /// Use and Equip raise events so the survival system and equipment system
    /// can subscribe when they are built, without touching this class.
    /// </summary>
    public sealed class ItemActionPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InventorySelection selection;
        [SerializeField] private PlayerInventory playerInventory;

        [Header("Buttons")]
        [SerializeField] private Button buttonUse;
        [SerializeField] private Button buttonSplit;
        [SerializeField] private Button buttonSort;
        [SerializeField] private Button buttonDelete;

        /// <summary>
        /// Raised when the player presses Use on a consumable item.
        /// The survival system subscribes here to apply HP/Hunger/Thirst.
        /// </summary>
        public event Action<ItemStack> OnUseConsumableRequested;

        /// <summary>
        /// Raised when the player presses Use on an equippable item.
        /// The equipment system subscribes here when it is built.
        /// </summary>
        public event Action<ItemStack> OnEquipRequested;

        // ── Unity lifecycle ──────────────────────────────────────────────────

        private void Start()
        {
            // Subscribe to inventory events here — guaranteed all Awake() calls
            // across all objects have already run, so Pockets is initialized.
            playerInventory.Pockets.OnInventoryChanged += RefreshSortButton;

            RefreshSelectionButtons(selection.SelectedSlot);
            RefreshSortButton();
        }

        private void OnEnable()
        {
            selection.OnSelectionChanged += RefreshSelectionButtons;
            buttonUse.onClick.AddListener(HandleUse);
            buttonSplit.onClick.AddListener(HandleSplit);
            buttonSort.onClick.AddListener(HandleSort);
            buttonDelete.onClick.AddListener(HandleDelete);
        }

        private void OnDisable()
        {
            selection.OnSelectionChanged -= RefreshSelectionButtons;

            // Guard against OnDisable firing before Start (e.g. object disabled on load).
            if (playerInventory != null && playerInventory.Pockets != null)
                playerInventory.Pockets.OnInventoryChanged -= RefreshSortButton;

            buttonUse.onClick.RemoveListener(HandleUse);
            buttonSplit.onClick.RemoveListener(HandleSplit);
            buttonSort.onClick.RemoveListener(HandleSort);
            buttonDelete.onClick.RemoveListener(HandleDelete);
        }

        // ── Refresh ──────────────────────────────────────────────────────────

        /// <summary>
        /// Updates Use, Split, Delete interactability based on the selected slot.
        /// </summary>
        private void RefreshSelectionButtons(SlotUI slot)
        {
            bool hasItem = slot != null && slot.HasItem;

            if (!hasItem)
            {
                buttonUse.interactable = false;
                buttonSplit.interactable = false;
                buttonDelete.interactable = false;
                return;
            }

            ItemStack stack = slot.CurrentStack;

            buttonUse.interactable = stack.ItemData.HasAbility<ConsumableAbility>()
                || stack.ItemData.HasAbility<EquipmentAbility>();

            buttonSplit.interactable = stack.ItemData.IsStackable && stack.Quantity > 1;

            buttonDelete.interactable = true;
        }

        /// <summary>
        /// Sort is enabled whenever pockets or backpack contains at least one item.
        /// Subscribed to OnInventoryChanged so it updates automatically.
        /// </summary>
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
            if (!TryGetSelectedStack(out ItemStack stack))
                return;

            ConsumableAbility consumable = stack.ItemData.GetAbility<ConsumableAbility>();
            if (consumable != null)
            {
                OnUseConsumableRequested?.Invoke(stack);
                ConsumeOne();
                return;
            }

            if (stack.ItemData.HasAbility<EquipmentAbility>())
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
            if (emptyIndex < 0)
                return;

            int splitAmount = stack.Quantity / 2;
            stack.RemoveQuantity(splitAmount);
            inventory.SetSlot(emptyIndex, new ItemStack(stack.ItemData, splitAmount));

            selection.Deselect();
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

            grid.BoundInventory.SetSlot(index, null);
            selection.Deselect();
        }

        // ── Private helpers ──────────────────────────────────────────────────

        private void ConsumeOne()
        {
            if (!TryFindSelectedLocation(out InventoryGridUI grid, out int index))
                return;

            InventorySystem inventory = grid.BoundInventory;
            ItemStack stack = inventory.GetSlot(index);
            if (stack == null)
                return;

            stack.RemoveQuantity(1);

            if (stack.IsEmpty)
                inventory.SetSlot(index, null);
            else
                inventory.NotifyChanged();

            selection.Deselect();
        }

        private bool TryGetSelectedStack(out ItemStack stack)
        {
            SlotUI slot = selection.SelectedSlot;
            if (slot != null && slot.HasItem)
            {
                stack = slot.CurrentStack;
                return true;
            }

            stack = null;
            return false;
        }

        private bool TryFindSelectedLocation(out InventoryGridUI foundGrid, out int foundIndex)
        {
            SlotUI slot = selection.SelectedSlot;
            if (slot != null)
            {
                InventoryGridUI grid = slot.GetComponentInParent<InventoryGridUI>();
                if (grid != null)
                {
                    int index = grid.IndexOf(slot);
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

        private static bool InventoryHasItem(InventorySystem inventory)
        {
            for (int i = 0; i < inventory.SlotCount; i++)
            {
                if (inventory.GetSlot(i) != null)
                    return true;
            }
            return false;
        }

        private static int FindEmptySlotExcluding(InventorySystem inventory, int excludeIndex)
        {
            for (int i = 0; i < inventory.SlotCount; i++)
            {
                if (i != excludeIndex && inventory.GetSlot(i) == null)
                    return i;
            }
            return -1;
        }
    }
}