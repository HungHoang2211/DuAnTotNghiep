using UnityEngine;

namespace SimpleSurvival.Items
{
    /// <summary>
    /// Owns the player's two inventories — pockets and backpack — and binds
    /// each to its grid UI on startup. This is the bridge between the data
    /// layer (InventorySystem) and the display layer (InventoryGridUI).
    ///
    /// Pockets are a fixed size. The backpack size changes with the equipped
    /// backpack; until the Equipment system exists it uses a default size.
    /// </summary>
    public sealed class PlayerInventory : MonoBehaviour
    {
        [Header("Pocket Inventory")]
        [SerializeField] private int pocketSlotCount = 10;
        [SerializeField] private InventoryGridUI pocketGridUI;

        [Header("Backpack Inventory")]
        [Tooltip("Backpack slots available before any backpack is equipped.")]
        [SerializeField] private int defaultBackpackSlotCount = 0;
        [SerializeField] private InventoryGridUI backpackGridUI;

        private InventorySystem pockets;
        private InventorySystem backpack;

        public InventorySystem Pockets => pockets;
        public InventorySystem Backpack => backpack;

        private void Awake()
        {
            pockets = new InventorySystem(pocketSlotCount);
            pocketGridUI.Bind(pockets);

            CreateBackpack(defaultBackpackSlotCount);
        }

        /// <summary>
        /// Rebuilds the backpack inventory with a new slot count — called by the
        /// Equipment system when a backpack is equipped or removed. Items that
        /// no longer fit are pushed into the pockets; whatever still does not
        /// fit is returned so the caller can drop it in the world.
        /// </summary>
        public int ResizeBackpack(int newSlotCount)
        {
            int overflow = 0;

            if (backpack != null)
            {
                overflow = MoveAllItemsToPockets();
            }

            CreateBackpack(newSlotCount);
            return overflow;
        }

        private void CreateBackpack(int slotCount)
        {
            if (slotCount > 0)
            {
                backpack = new InventorySystem(slotCount);
            }
            else
            {
                backpack = new InventorySystem(1);
            }

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

                int notFitted = pockets.AddItem(stack.ItemData, stack.Quantity);
                overflow += notFitted;
            }

            return overflow;
        }
    }
}
