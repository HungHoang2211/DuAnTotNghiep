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
    /// Other systems (the action buttons, the item info panel) read
    /// SelectedSlot to know which cell the player is acting on.
    /// </summary>
    public sealed class InventorySelection : MonoBehaviour
    {
        [Tooltip("Every SlotUI the player can select — pockets and backpack cells.")]
        [SerializeField] private List<SlotUI> allCells = new List<SlotUI>();

        [Tooltip("When the list above is empty, collect SlotUI from children.")]
        [SerializeField] private bool autoCollectFromChildren = true;

        private SlotUI selectedSlot;

        /// <summary>The currently selected cell, or null if nothing is selected.</summary>
        public SlotUI SelectedSlot => selectedSlot;

        /// <summary>Raised whenever the selection changes. Argument may be null.</summary>
        public event Action<SlotUI> OnSelectionChanged;

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
            }
        }

        private void OnDisable()
        {
            foreach (SlotUI cell in allCells)
            {
                cell.OnClicked -= HandleCellClicked;
            }
        }

        /// <summary>
        /// Selects a cell: deselects the previous one, highlights the new one.
        /// Clicking the already-selected cell deselects it (toggle).
        /// </summary>
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
            {
                selectedSlot.SetSelected(false);
            }

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
    }
}