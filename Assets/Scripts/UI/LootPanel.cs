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

        [Header("Buttons")]
        [SerializeField] private Button takeAllButton;
        [SerializeField] private Button sortButton;

        [Header("Player Inventory")]
        [SerializeField] private PlayerInventory playerInventory;

        private LootContainer _container;

        public LootContainer Container => _container;
        public InventoryGridUI Grid => lootGrid;
        public InventorySelection Selection => lootSelection;

        private void Awake()
        {
            if (panelRoot != null) panelRoot.SetActive(false);

            if (takeAllButton != null)
                takeAllButton.onClick.AddListener(HandleTakeAll);

            if (sortButton != null)
                sortButton.onClick.AddListener(HandleSort);
        }

        private void OnDestroy()
        {
            if (takeAllButton != null)
                takeAllButton.onClick.RemoveListener(HandleTakeAll);

            if (sortButton != null)
                sortButton.onClick.RemoveListener(HandleSort);
        }

        public void Show(LootContainer container)
        {
            if (container == null) return;

            UnsubscribeContainer();

            _container = container;
            SubscribeContainer();

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

            if (panelRoot != null) panelRoot.SetActive(true);

            if (lootGrid != null)
                lootGrid.Bind(_container.Inventory);

            RefreshButtons();
        }

        public void Hide()
        {
            UnsubscribeContainer();

            if (lootGrid != null) lootGrid.Unbind();

            if (lootSelection != null) lootSelection.Deselect();

            _container = null;

            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void SubscribeContainer()
        {
            if (_container == null) return;
            _container.OnLooted += HandleContainerLooted;
            _container.OnDestroyed += HandleContainerDestroyed;
        }

        private void UnsubscribeContainer()
        {
            if (_container == null) return;
            _container.OnLooted -= HandleContainerLooted;
            _container.OnDestroyed -= HandleContainerDestroyed;
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
                    continue;
            }
        }

        private void HandleSort()
        {
            if (_container == null) return;
            if (_container.Inventory == null) return;
            _container.Inventory.Sort();
            if (lootSelection != null) lootSelection.Deselect();
        }

        private void RefreshButtons()
        {

            
            if (_container == null) return;

            bool hasItem = !_container.IsEmpty;

            Debug.Log($"[LootPanel] RefreshButtons on {_container.name} — hasItem={hasItem}, slotCount={_container.SlotCount}");
            if (takeAllButton != null)
                takeAllButton.interactable = hasItem;

            if (sortButton != null)
                sortButton.interactable = hasItem;
        }
    }
}