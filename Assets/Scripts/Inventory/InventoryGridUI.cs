using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.Items
{
    /// <summary>
    /// Connects one InventorySystem to a row of SlotUI cells. Listens for the
    /// inventory's change event and redraws the cells. Reusable: the player's
    /// pockets, the backpack, and loot containers each use their own instance.
    ///
    /// Cells can be assigned by hand in the Inspector, or left empty — in which
    /// case the grid collects every SlotUI found in its children, in Hierarchy
    /// order. Cells beyond the inventory's slot count are shown as locked.
    /// </summary>
    public sealed class InventoryGridUI : MonoBehaviour
    {
        [Header("Cells")]
        [Tooltip("SlotUI cells in order. Leave empty to auto-collect from children.")]
        [SerializeField] private List<SlotUI> cells = new List<SlotUI>();

        [Tooltip("When Cells is empty, collect SlotUI components from children.")]
        [SerializeField] private bool autoCollectFromChildren = true;

        private InventorySystem boundInventory;

        private void Awake()
        {
            if (cells.Count == 0 && autoCollectFromChildren)
            {
                CollectCellsFromChildren();
            }
        }

        /// <summary>
        /// Fills the cell list with every SlotUI found under this object,
        /// in Hierarchy order. Used when cells are not assigned by hand.
        /// </summary>
        private void CollectCellsFromChildren()
        {
            cells.Clear();
            cells.AddRange(GetComponentsInChildren<SlotUI>(includeInactive: true));
        }

        /// <summary>
        /// Binds this grid to an inventory and draws it. Call again with a
        /// different inventory to reuse the same grid (e.g. opening a chest).
        /// </summary>
        public void Bind(InventorySystem inventory)
        {
            Unbind();

            boundInventory = inventory;

            if (boundInventory != null)
            {
                boundInventory.OnInventoryChanged += Redraw;
            }

            Redraw();
        }

        /// <summary>Stops listening to the current inventory.</summary>
        public void Unbind()
        {
            if (boundInventory != null)
            {
                boundInventory.OnInventoryChanged -= Redraw;
                boundInventory = null;
            }
        }

        /// <summary>The inventory this grid is bound to, or null if none.</summary>
        public InventorySystem BoundInventory => boundInventory;

        /// <summary>
        /// Returns the slot index of a cell within this grid, or -1 if the cell
        /// does not belong here. Drag-drop uses this to translate "the player
        /// dropped on this cell" into "slot N of this inventory".
        /// </summary>
        public int IndexOf(SlotUI cell)
        {
            return cells.IndexOf(cell);
        }

        private void OnDestroy()
        {
            Unbind();
        }

        /// <summary>
        /// Redraws every cell. A cell within the inventory's slot range shows
        /// that slot's stack; a cell beyond the range is shown as locked.
        /// </summary>
        private void Redraw()
        {
            int activeSlots = boundInventory != null ? boundInventory.SlotCount : 0;

            for (int i = 0; i < cells.Count; i++)
            {
                SlotUI cell = cells[i];

                if (i < activeSlots)
                {
                    cell.SetLocked(false);
                    cell.SetStack(boundInventory.GetSlot(i));
                }
                else
                {
                    cell.SetLocked(true);
                }
            }
        }
    }
}