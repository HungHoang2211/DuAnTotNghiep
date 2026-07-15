using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SimpleSurvival.Items;
using SimpleSurvival.Loot;

namespace SimpleSurvival.UI
{
    public sealed class LootPanel : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject panelRoot;

        [Header("Title")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Image titleIcon;

        [Header("Grid")]
        [SerializeField] private InventoryGridUI lootGrid;

        [Header("Selection")]
        [SerializeField] private InventorySelection lootSelection;
        [SerializeField] private InventorySelection inventorySelection;

        [Header("Buttons")]
        [SerializeField] private Button takeAllButton;
        [SerializeField] private Button putAllButton;
        [SerializeField] private Button sortButton;

        [Header("Notify")]
        [SerializeField] private GameObject notifyRoot;
        [SerializeField] private TMP_Text notifyText;
        [SerializeField] private float notifyDuration = 2f;

        [Header("Player Inventory")]
        [SerializeField] private PlayerInventory playerInventory;

        private LootContainer _container;
        private InventorySystem _subscribedBackpack;

        public LootContainer Container => _container;
        public InventoryGridUI Grid => lootGrid;
        public InventorySelection Selection => lootSelection;

        private void Awake()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            if (notifyRoot != null) notifyRoot.SetActive(false);

            if (takeAllButton != null)
                takeAllButton.onClick.AddListener(HandleTakeAll);

            if (putAllButton != null)
                putAllButton.onClick.AddListener(HandlePutAll);

            if (sortButton != null)
                sortButton.onClick.AddListener(HandleSort);
        }

        private void OnDestroy()
        {
            if (takeAllButton != null)
                takeAllButton.onClick.RemoveListener(HandleTakeAll);

            if (putAllButton != null)
                putAllButton.onClick.RemoveListener(HandlePutAll);

            if (sortButton != null)
                sortButton.onClick.RemoveListener(HandleSort);

            CancelInvoke(nameof(HideNotify));
        }

        public void Show(LootContainer container)
        {
            if (container == null) return;

            UnsubscribeContainer();
            UnsubscribePlayerInventory();

            _container = container;
            SubscribeContainer();
            SubscribePlayerInventory();

            if (titleText != null)
                titleText.text = _container.DisplayName;

            if (titleIcon != null)
            {
                Sprite icon = _container.DisplayIcon;
                if (icon != null)
                {
                    titleIcon.sprite = icon;
                    titleIcon.enabled = true;
                }
                else
                {
                    titleIcon.enabled = false;
                }
            }

            if (lootGrid != null)
                lootGrid.Bind(_container.Inventory);

            RefreshButtons();

            if (panelRoot != null) panelRoot.SetActive(true);
        }

        public void Hide()
        {
            UnsubscribeContainer();
            UnsubscribePlayerInventory();

            if (lootGrid != null) lootGrid.Unbind();

            if (lootSelection != null) lootSelection.Deselect();

            CancelInvoke(nameof(HideNotify));
            HideNotify();

            _container = null;

            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void SubscribeContainer()
        {
            if (_container == null) return;
            _container.OnLooted += HandleContainerLooted;
            _container.OnDestroyed += HandleContainerDestroyed;

            if (lootSelection != null)
                lootSelection.OnCellDoubleClicked += HandleLootCellDoubleClicked;
        }

        private void UnsubscribeContainer()
        {
            if (_container == null) return;
            _container.OnLooted -= HandleContainerLooted;
            _container.OnDestroyed -= HandleContainerDestroyed;

            if (lootSelection != null)
                lootSelection.OnCellDoubleClicked -= HandleLootCellDoubleClicked;
        }

        private void SubscribePlayerInventory()
        {
            if (playerInventory == null) return;

            playerInventory.Pockets.OnInventoryChanged += RefreshButtons;
            playerInventory.OnBackpackReplaced += HandleBackpackReplaced;
            HandleBackpackReplaced();

            if (inventorySelection != null)
                inventorySelection.OnCellDoubleClicked += HandleInventoryCellDoubleClicked;
        }

        private void UnsubscribePlayerInventory()
        {
            if (playerInventory != null)
            {
                playerInventory.Pockets.OnInventoryChanged -= RefreshButtons;
                playerInventory.OnBackpackReplaced -= HandleBackpackReplaced;
            }

            if (_subscribedBackpack != null)
                _subscribedBackpack.OnInventoryChanged -= RefreshButtons;
            _subscribedBackpack = null;

            if (inventorySelection != null)
                inventorySelection.OnCellDoubleClicked -= HandleInventoryCellDoubleClicked;
        }

        private void HandleBackpackReplaced()
        {
            if (_subscribedBackpack != null)
                _subscribedBackpack.OnInventoryChanged -= RefreshButtons;

            _subscribedBackpack = playerInventory.Backpack;

            if (_subscribedBackpack != null)
                _subscribedBackpack.OnInventoryChanged += RefreshButtons;

            RefreshButtons();
        }

        private void HandleContainerLooted(LootContainer container)
        {
            RefreshButtons();
        }

        private void HandleContainerDestroyed(SimpleSurvival.Targets.ITargetable target)
        {
            if (InventoryPanelController.Instance != null)
                InventoryPanelController.Instance.Close();
        }

        private void HandleTakeAll()
        {
            if (_container == null || playerInventory == null) return;

            InventorySystem source = _container.Inventory;
            if (source == null) return;

            bool blockedByFullInventory = false;

            for (int i = 0; i < source.SlotCount; i++)
            {
                ItemStack stack = source.GetSlot(i);
                if (stack == null) continue;

                int before = stack.Quantity;
                int overflow = playerInventory.Pockets.AddStack(stack);
                if (overflow > 0 && playerInventory.Backpack != null)
                    overflow = playerInventory.Backpack.AddStack(stack);

                if (overflow == 0)
                    source.SetSlot(i, null);
                else if (overflow < before)
                    source.NotifyChanged();
                else
                    blockedByFullInventory = true;
            }

            if (blockedByFullInventory)
                ShowNotify("Inventory full");
        }

        private void HandlePutAll()
        {
            if (_container == null || playerInventory == null) return;

            InventorySystem target = _container.Inventory;
            if (target == null) return;

            bool blockedByFullLoot = PutAllFrom(playerInventory.Pockets, target);
            if (playerInventory.Backpack != null)
                blockedByFullLoot |= PutAllFrom(playerInventory.Backpack, target);

            if (blockedByFullLoot)
                ShowNotify("Loot full");
        }

        private static bool PutAllFrom(InventorySystem source, InventorySystem target)
        {
            bool blocked = false;

            for (int i = 0; i < source.SlotCount; i++)
            {
                ItemStack stack = source.GetSlot(i);
                if (stack == null) continue;

                int before = stack.Quantity;
                int overflow = target.AddStack(stack);

                if (overflow == 0)
                    source.SetSlot(i, null);
                else if (overflow < before)
                    source.NotifyChanged();
                else
                    blocked = true;
            }

            return blocked;
        }

        private void HandleInventoryCellDoubleClicked(CellUI cell)
        {
            if (_container == null || playerInventory == null || !cell.HasItem) return;

            InventoryGridUI grid = cell.GetComponentInParent<InventoryGridUI>();
            if (grid == null) return;

            int index = grid.IndexOf(cell);
            if (index < 0) return;

            InventorySystem source = grid.BoundInventory;
            ItemStack stack = source.GetSlot(index);
            if (stack == null) return;

            int before = stack.Quantity;
            int overflow = _container.Inventory.AddStack(stack);

            if (overflow == 0)
                source.SetSlot(index, null);
            else if (overflow < before)
                source.NotifyChanged();
            else
                ShowNotify("Loot full");
        }

        private void HandleLootCellDoubleClicked(CellUI cell)
        {
            if (_container == null || playerInventory == null || !cell.HasItem) return;
            if (lootGrid == null) return;

            int index = lootGrid.IndexOf(cell);
            if (index < 0) return;

            InventorySystem source = _container.Inventory;
            ItemStack stack = source.GetSlot(index);
            if (stack == null) return;

            int before = stack.Quantity;
            int overflow = playerInventory.Pockets.AddStack(stack);
            if (overflow > 0 && playerInventory.Backpack != null)
                overflow = playerInventory.Backpack.AddStack(stack);

            if (overflow == 0)
                source.SetSlot(index, null);
            else if (overflow < before)
                source.NotifyChanged();
            else
                ShowNotify("Inventory full");
        }

        private void HandleSort()
        {
            if (_container == null) return;
            if (_container.Inventory == null) return;

            bool changed = _container.Inventory.Sort();
            if (lootSelection != null) lootSelection.Deselect();

            if (!changed)
                ShowNotify("Nothing to sort");
        }

        private void RefreshButtons()
        {
            if (_container == null) return;

            bool hasLootItem = !_container.IsEmpty;
            bool hasPlayerItem = playerInventory != null && HasAnyItem(playerInventory);

            if (takeAllButton != null)
                takeAllButton.interactable = hasLootItem;

            if (sortButton != null)
                sortButton.interactable = hasLootItem;

            if (putAllButton != null)
                putAllButton.interactable = hasPlayerItem;
        }

        private void ShowNotify(string message)
        {
            if (notifyRoot == null) return;

            CancelInvoke(nameof(HideNotify));
            if (notifyText != null) notifyText.text = message;
            notifyRoot.SetActive(true);
            Invoke(nameof(HideNotify), notifyDuration);
        }

        private void HideNotify()
        {
            if (notifyRoot != null) notifyRoot.SetActive(false);
        }

        private static bool HasAnyItem(PlayerInventory playerInventory)
        {
            if (InventoryHasItem(playerInventory.Pockets)) return true;
            return playerInventory.Backpack != null && InventoryHasItem(playerInventory.Backpack);
        }

        private static bool InventoryHasItem(InventorySystem inventory)
        {
            for (int i = 0; i < inventory.SlotCount; i++)
                if (inventory.GetSlot(i) != null) return true;
            return false;
        }
    }
}