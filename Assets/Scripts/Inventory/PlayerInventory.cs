using UnityEngine;

namespace SimpleSurvival.Items
{
    /// <summary>
    /// Owns the player's two inventories — pockets and backpack — and binds
    /// each to its grid UI on startup. This is the bridge between the data
    /// layer (InventorySystem) and the display layer (InventoryGridUI).
    ///
    /// Pockets are a fixed size. The backpack exists only while a backpack is
    /// equipped; with no backpack it is null and every backpack cell shows as
    /// locked. The backpack can never have more slots than the UI has cells —
    /// otherwise items would enter data slots that have no cell to display them.
    /// </summary>
    public sealed class PlayerInventory : MonoBehaviour
    {
        [Header("Pocket Inventory")]
        [SerializeField] private int pocketSlotCount = 10;
        [SerializeField] private InventoryGridUI pocketGridUI;

        [Header("Backpack Inventory")]
        [Tooltip("Backpack slots before any backpack is equipped. 0 = no backpack.")]
        [SerializeField] private int defaultBackpackSlotCount = 0;

        [Tooltip("Maximum backpack slots. MUST equal the number of cells in the "
            + "backpack grid UI, or items can fall into cells that do not exist.")]
        [SerializeField] private int maxBackpackSlotCount = 20;

        [SerializeField] private InventoryGridUI backpackGridUI;

        private InventorySystem pockets;
        private InventorySystem backpack;

        public InventorySystem Pockets => pockets;

        /// <summary>The backpack inventory, or null when no backpack is equipped.</summary>
        public InventorySystem Backpack => backpack;

        public int MaxBackpackSlotCount => maxBackpackSlotCount;

        private void Awake()
        {
            pockets = new InventorySystem(pocketSlotCount);
            pocketGridUI.Bind(pockets);

            SetBackpack(defaultBackpackSlotCount);
        }

        /// <summary>
        /// Rebuilds the backpack with a new slot count — called by the Equipment
        /// system when a backpack is equipped or removed. The count is clamped
        /// to [0, maxBackpackSlotCount]. A count of 0 means no backpack. Items
        /// that no longer fit are pushed into the pockets; whatever still does
        /// not fit is returned so the caller can drop it in the world.
        /// </summary>
        public int ResizeBackpack(int newSlotCount)
        {
            newSlotCount = Mathf.Clamp(newSlotCount, 0, maxBackpackSlotCount);

            int overflow = 0;

            if (backpack != null)
            {
                overflow = MoveAllItemsToPockets();
            }

            SetBackpack(newSlotCount);
            return overflow;
        }

        /// <summary>
        /// Creates the backpack inventory, or sets it to null when the count is
        /// 0, then binds the result to the UI. Binding null locks every cell.
        /// </summary>
        private void SetBackpack(int slotCount)
        {
            backpack = slotCount > 0 ? new InventorySystem(slotCount) : null;
            backpackGridUI.Bind(backpack);
        }

        /// <summary>
        /// Empties the current backpack into the pockets. Returns the amount
        /// that did not fit anywhere.
        /// </summary>
        private int MoveAllItemsToPockets()
        {
            int overflow = 0;

            for (int i = 0; i < backpack.SlotCount; i++)
            {
                ItemStack stack = backpack.GetSlot(i);
                if (stack == null)
                {
                    continue;
                }

                overflow += pockets.AddItem(stack.ItemData, stack.Quantity);
            }

            return overflow;
        }
    }
}