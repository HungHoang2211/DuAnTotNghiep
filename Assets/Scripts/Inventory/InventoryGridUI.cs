using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.Items
{
    /// <summary>
    /// Connects one InventorySystem to a row of CellUI cells. Listens for the
    /// inventory's change event and redraws the cells. Reusable: the player's
    /// pockets, the backpack, and loot containers each use their own instance.
    /// </summary>
    public sealed class InventoryGridUI : MonoBehaviour
    {
        [Header("Cells")]
        [Tooltip("CellUI cells in order. Leave empty to auto-collect from children.")]
        [SerializeField] private List<CellUI> cells = new List<CellUI>();

        [Tooltip("When Cells is empty, collect CellUI components from children.")]
        [SerializeField] private bool autoCollectFromChildren = true;

        private InventorySystem _boundInventory;

        public InventorySystem BoundInventory => _boundInventory;

        private void Awake()
        {
            if (cells.Count == 0 && autoCollectFromChildren)
                CollectCellsFromChildren();
        }

        /// <summary>
        /// Binds this grid to an inventory and draws it. Call again with a
        /// different inventory to reuse the same grid (e.g. opening a chest).
        /// </summary>
        public void Bind(InventorySystem inventory)
        {
            Unbind();

            _boundInventory = inventory;

            if (_boundInventory != null)
                _boundInventory.OnInventoryChanged += Redraw;

            Redraw();
        }

        public void Unbind()
        {
            if (_boundInventory != null)
            {
                _boundInventory.OnInventoryChanged -= Redraw;
                _boundInventory = null;
            }
        }

        public int IndexOf(CellUI cell)
        {
            return cells.IndexOf(cell);
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void CollectCellsFromChildren()
        {
            cells.Clear();
            cells.AddRange(GetComponentsInChildren<CellUI>(includeInactive: true));
        }

        private void Redraw()
        {
            int activeSlots = _boundInventory != null ? _boundInventory.SlotCount : 0;

            for (int i = 0; i < cells.Count; i++)
            {
                CellUI cell = cells[i];

                if (i < activeSlots)
                {
                    cell.SetLocked(false);
                    cell.SetStack(_boundInventory.GetSlot(i));
                }
                else
                {
                    cell.SetLocked(true);
                }
            }
        }
    }
}