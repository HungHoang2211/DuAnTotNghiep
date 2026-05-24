using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.Items
{
    /// <summary>
    /// Connects one InventorySystem to a row of SlotUI cells. Listens for the
    /// inventory's change event and redraws the cells. Reusable: the player's
    /// pockets, the backpack, and loot containers each use their own instance.
    ///
    /// The SlotUI cells are placed in the scene by hand and assigned here.
    /// Cells beyond the inventory's slot count are shown as locked.
    /// </summary>
    public sealed class InventoryGridUI : MonoBehaviour
    {
        [Header("Cells")]
        [Tooltip("All SlotUI cells of this grid, in order. Assigned by hand.")]
        [SerializeField] private List<SlotUI> cells = new List<SlotUI>();

        private InventorySystem boundInventory;

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
