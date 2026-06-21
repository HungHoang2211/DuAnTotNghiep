using UnityEngine;

namespace SimpleSurvival.Items
{
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

        public InventorySystem Backpack => backpack;

        public int MaxBackpackSlotCount => maxBackpackSlotCount;

        private void Awake()
        {
            pockets = new InventorySystem(pocketSlotCount);
            pocketGridUI.Bind(pockets);

            SetBackpack(defaultBackpackSlotCount);
        }

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

        private void SetBackpack(int slotCount)
        {
            backpack = slotCount > 0 ? new InventorySystem(slotCount) : null;
            backpackGridUI.Bind(backpack);
        }

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

                overflow += pockets.AddStack(stack);
            }

            return overflow;
        }
    }
}