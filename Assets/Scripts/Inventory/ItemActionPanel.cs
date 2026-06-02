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

        [Header("Dialogs")]
        [SerializeField] private SimpleSurvival.UI.ConfirmDeleteDialog confirmDeleteDialog;

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
            playerInventory.Pockets.OnInventoryChanged += RefreshSelectionButtonsFromInventory;

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
            {
                playerInventory.Pockets.OnInventoryChanged -= RefreshSortButton;
                playerInventory.Pockets.OnInventoryChanged -= RefreshSelectionButtonsFromInventory;
            }

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
                || stack.ItemData.HasAbility<EquipmentAbility>()
                || stack.ItemData.HasAbility<WeaponAbility>();

            // Find the owning inventory to check free slots for split.
            InventoryGridUI splitGrid = slot.GetComponentInParent<InventoryGridUI>();
            bool hasFreeSlot = splitGrid != null
                && HasFreeSlotForSplit(splitGrid.BoundInventory,
                    splitGrid.IndexOf(slot));

            buttonSplit.interactable = stack.ItemData.IsStackable
                && stack.Quantity > 1
                && hasFreeSlot;

            buttonDelete.interactable = true;
        }

        /// <summary>
        /// Sort is enabled whenever pockets or backpack contains at least one item.
        /// Subscribed to OnInventoryChanged so it updates automatically.
        /// </summary>
        private void RefreshSelectionButtonsFromInventory()
        {
            RefreshSelectionButtons(selection.SelectedSlot);
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

            // Try to find empty slot in current inventory first, then the other one.
            int emptyIndex = FindEmptySlotExcluding(inventory, index);
            InventorySystem targetInventory = inventory;

            if (emptyIndex < 0)
            {
                // Try the other inventory (pocket → backpack or vice versa).
                InventorySystem other = GetOtherInventory(inventory);
                if (other != null)
                {
                    emptyIndex = FindEmptySlotExcluding(other, -1);
                    targetInventory = other;
                }
            }

            if (emptyIndex < 0)
                return;

            int splitAmount = stack.Quantity / 2;
            stack.RemoveQuantity(splitAmount);
            targetInventory.SetSlot(emptyIndex, new ItemStack(stack.ItemData, splitAmount));

            // Keep selection on the original stack so player can keep splitting.
            // Deselect only when stack can no longer be split.
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
            if (stack == null)
                return;

            string itemName = stack.ItemData.ItemName;
            confirmDeleteDialog.Show(
                $"Delete {itemName}?",
                confirmed =>
                {
                    if (!confirmed)
                        return;

                    // Re-validate vì player có thể đã thay đổi selection trong lúc dialog hiện
                    if (!TryFindSelectedLocation(out InventoryGridUI g, out int i))
                        return;

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

        /// <summary>
        /// Returns the other inventory — if current is Pockets returns Backpack
        /// and vice versa. Returns null if backpack is not equipped.
        /// </summary>
        /// <summary>
        /// Returns true when there is at least one free slot available for the
        /// split result — either in the current inventory or the other one.
        /// </summary>
        private bool HasFreeSlotForSplit(InventorySystem inventory, int excludeIndex)
        {
            if (FindEmptySlotExcluding(inventory, excludeIndex) >= 0)
                return true;

            InventorySystem other = GetOtherInventory(inventory);
            return other != null && FindEmptySlotExcluding(other, -1) >= 0;
        }

        private InventorySystem GetOtherInventory(InventorySystem current)
        {
            if (current == playerInventory.Pockets)
                return playerInventory.Backpack;

            if (current == playerInventory.Backpack)
                return playerInventory.Pockets;

            return null;
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