using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.Items
{
    /// <summary>
    /// Coordinates cell selection across the whole inventory. Only one cell may
    /// be selected at a time — even though pockets and backpack are separate
    /// grids — so this single object listens to every cell and keeps exactly
    /// one highlighted.
    ///
    /// Also bridges SlotUI hold events to the ItemInfoPanel tooltip.
    /// </summary>
    public sealed class InventorySelection : MonoBehaviour
    {
        [Header("Cells")]
        [Tooltip("Every SlotUI the player can select — pockets and backpack cells.")]
        [SerializeField] private List<SlotUI> allCells = new List<SlotUI>();

        [Tooltip("When the list above is empty, collect SlotUI from children.")]
        [SerializeField] private bool autoCollectFromChildren = true;

        [Header("Tooltip")]
        [SerializeField] private ItemInfoPanel itemInfoPanel;

        private SlotUI selectedSlot;

        /// <summary>The currently selected cell, or null if nothing is selected.</summary>
        public SlotUI SelectedSlot => selectedSlot;

        /// <summary>Raised whenever the selection changes. Argument may be null.</summary>
        public event Action<SlotUI> OnSelectionChanged;

        // ── Unity lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            if (allCells.Count == 0 && autoCollectFromChildren)
            {
                allCells.Clear();
                allCells.AddRange(GetComponentsInChildren<SlotUI>(includeInactive: true));
            }
        }

        private void OnEnable()
        {
            foreach (SlotUI cell in allCells)
            {
                cell.OnClicked += HandleCellClicked;
                cell.OnDoubleClicked += HandleCellDoubleClicked;
                cell.OnHeld += HandleCellHeld;
                cell.OnReleased += HandleCellReleased;
            }
        }

        private void OnDisable()
        {
            foreach (SlotUI cell in allCells)
            {
                cell.OnClicked -= HandleCellClicked;
                cell.OnDoubleClicked -= HandleCellDoubleClicked;
                cell.OnHeld -= HandleCellHeld;
                cell.OnReleased -= HandleCellReleased;
            }
        }

        // ── Selection ────────────────────────────────────────────────────────

        private void HandleCellClicked(SlotUI cell)
        {
            if (selectedSlot == cell)
            {
                Deselect();
                return;
            }

            Select(cell);
        }

        public void Select(SlotUI cell)
        {
            if (selectedSlot != null)
                selectedSlot.SetSelected(false);

            selectedSlot = cell;
            selectedSlot.SetSelected(true);
            OnSelectionChanged?.Invoke(selectedSlot);
        }

        /// <summary>Clears the selection — nothing is highlighted afterwards.</summary>
        public void Deselect()
        {
            if (selectedSlot != null)
            {
                selectedSlot.SetSelected(false);
                selectedSlot = null;
            }

            OnSelectionChanged?.Invoke(null);
        }

        /// <summary>Raised when a cell is double-clicked — for auto-equip.</summary>
        public event System.Action<SlotUI> OnCellDoubleClicked;

        private void HandleCellDoubleClicked(SlotUI cell)
        {
            OnCellDoubleClicked?.Invoke(cell);
        }

        // ── Tooltip ──────────────────────────────────────────────────────────

        private void HandleCellHeld(SlotUI cell)
        {
            if (itemInfoPanel == null || !cell.HasItem)
                return;

            itemInfoPanel.Show(cell.CurrentStack, (RectTransform)cell.transform);
        }

        private void HandleCellReleased(SlotUI cell)
        {
            if (itemInfoPanel == null)
                return;

            itemInfoPanel.Hide();
        }
    }
}