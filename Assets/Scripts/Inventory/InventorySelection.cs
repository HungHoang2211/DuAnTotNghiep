using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.Items
{
    /// <summary>
    /// Coordinates cell selection across the whole inventory. Only one cell may
    /// be selected at a time — even across pockets and backpack grids.
    /// Also bridges hold events to the ItemInfoPanel tooltip.
    /// </summary>
    public sealed class InventorySelection : MonoBehaviour
    {
        [Header("Cells")]
        [Tooltip("Every CellUI the player can select — pockets and backpack cells.")]
        [SerializeField] private List<CellUI> allCells = new List<CellUI>();

        [Tooltip("When the list above is empty, collect CellUI from children.")]
        [SerializeField] private bool autoCollectFromChildren = true;

        [Header("Tooltip")]
        [SerializeField] private ItemInfoPanel itemInfoPanel;

        private CellUI _selectedCell;

        public CellUI SelectedCell => _selectedCell;

        public event Action<CellUI> OnSelectionChanged;
        public event Action<CellUI> OnCellDoubleClicked;

        // ── Unity lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            if (allCells.Count == 0 && autoCollectFromChildren)
            {
                allCells.Clear();
                allCells.AddRange(GetComponentsInChildren<CellUI>(includeInactive: true));
            }
        }

        private void OnEnable()
        {
            foreach (CellUI cell in allCells)
            {
                cell.OnClicked += HandleCellClicked;
                cell.OnDoubleClicked += HandleCellDoubleClicked;
                cell.OnHeld += HandleCellHeld;
                cell.OnReleased += HandleCellReleased;
            }
        }

        private void OnDisable()
        {
            foreach (CellUI cell in allCells)
            {
                cell.OnClicked -= HandleCellClicked;
                cell.OnDoubleClicked -= HandleCellDoubleClicked;
                cell.OnHeld -= HandleCellHeld;
                cell.OnReleased -= HandleCellReleased;
            }
        }

        // ── Selection ────────────────────────────────────────────────────────

        private void HandleCellClicked(CellUI cell)
        {
            if (_selectedCell == cell)
            {
                Deselect();
                return;
            }

            Select(cell);
        }

        public void Select(CellUI cell)
        {
            if (_selectedCell != null)
                _selectedCell.SetSelected(false);

            _selectedCell = cell;
            _selectedCell.SetSelected(true);
            OnSelectionChanged?.Invoke(_selectedCell);
        }

        public void Deselect()
        {
            if (_selectedCell != null)
            {
                _selectedCell.SetSelected(false);
                _selectedCell = null;
            }

            OnSelectionChanged?.Invoke(null);
        }

        private void HandleCellDoubleClicked(CellUI cell)
        {
            OnCellDoubleClicked?.Invoke(cell);
        }

        // ── Tooltip ──────────────────────────────────────────────────────────

        private void HandleCellHeld(CellUI cell)
        {
            if (itemInfoPanel == null || !cell.HasItem)
                return;

            itemInfoPanel.Show(cell.CurrentStack, (RectTransform)cell.transform);
        }

        private void HandleCellReleased(CellUI cell)
        {
            if (itemInfoPanel != null)
                itemInfoPanel.Hide();
        }
    }
}