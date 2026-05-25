using UnityEngine;

namespace SimpleSurvival.Items
{
    /// <summary>
    /// Temporary test harness for the inventory. Draws on-screen buttons to add
    /// items, resize the backpack, and wear down item durability — so the
    /// inventory can be exercised before the real interaction and combat
    /// systems exist.
    ///
    /// This is a development tool, not a game system. Remove it before release.
    /// </summary>
    public sealed class InventoryTester : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInventory playerInventory;

        [Header("Test Items")]
        [Tooltip("A stackable item, e.g. wood.")]
        [SerializeField] private ItemData stackableItem;

        [Tooltip("A durable item, e.g. an axe.")]
        [SerializeField] private ItemData durableItem;

        [Header("Settings")]
        [SerializeField] private int addAmount = 5;
        [SerializeField] private int backpackStep = 5;

        private int currentBackpackSlots;

        private void Awake()
        {
            currentBackpackSlots = 0;
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 260, 400), GUI.skin.box);
            GUILayout.Label("INVENTORY TESTER");

            DrawAddItemButtons();
            GUILayout.Space(8);
            DrawBackpackButtons();
            GUILayout.Space(8);
            DrawDurabilityButton();

            GUILayout.EndArea();
        }

        private void DrawAddItemButtons()
        {
            GUILayout.Label("Add Items");

            if (GUILayout.Button($"Add {addAmount} stackable -> Pockets"))
            {
                AddToPockets(stackableItem);
            }

            if (GUILayout.Button("Add 1 durable -> Pockets"))
            {
                AddToPockets(durableItem, 1);
            }

            if (GUILayout.Button($"Add {addAmount} stackable -> Backpack"))
            {
                AddToBackpack(stackableItem);
            }
        }

        private void DrawBackpackButtons()
        {
            int max = playerInventory.MaxBackpackSlotCount;
            GUILayout.Label($"Backpack slots: {currentBackpackSlots} / {max}");

            // The + button stops at the maximum so items can never enter
            // backpack data slots that have no UI cell to display them.
            using (new GUIEnabledScope(currentBackpackSlots < max))
            {
                if (GUILayout.Button($"+{backpackStep} backpack slots"))
                {
                    ResizeBackpack(currentBackpackSlots + backpackStep);
                }
            }

            using (new GUIEnabledScope(currentBackpackSlots > 0))
            {
                if (GUILayout.Button($"-{backpackStep} backpack slots"))
                {
                    ResizeBackpack(currentBackpackSlots - backpackStep);
                }
            }
        }

        private void DrawDurabilityButton()
        {
            GUILayout.Label("Durability");

            if (GUILayout.Button("Reduce durability of first durable item"))
            {
                ReduceFirstDurableItem();
            }
        }

        private void AddToPockets(ItemData item, int amount = -1)
        {
            if (item == null)
            {
                Debug.LogWarning("Tester: test item not assigned.");
                return;
            }

            int used = amount > 0 ? amount : addAmount;
            int overflow = playerInventory.Pockets.AddItem(item, used);

            if (overflow > 0)
            {
                Debug.Log($"Pockets full — {overflow} item(s) did not fit.");
            }
        }

        private void AddToBackpack(ItemData item)
        {
            if (item == null)
            {
                Debug.LogWarning("Tester: test item not assigned.");
                return;
            }

            if (playerInventory.Backpack == null)
            {
                Debug.Log("No backpack equipped — add backpack slots first.");
                return;
            }

            int overflow = playerInventory.Backpack.AddItem(item, addAmount);

            if (overflow > 0)
            {
                Debug.Log($"Backpack full — {overflow} item(s) did not fit.");
            }
        }

        private void ResizeBackpack(int newSlotCount)
        {
            int overflow = playerInventory.ResizeBackpack(newSlotCount);

            // ResizeBackpack clamps to the allowed range; mirror the clamped
            // value here so the label stays in sync.
            currentBackpackSlots = Mathf.Clamp(newSlotCount, 0,
                playerInventory.MaxBackpackSlotCount);

            if (overflow > 0)
            {
                Debug.Log($"Backpack shrunk — {overflow} item(s) did not fit "
                    + "into the pockets and were lost.");
            }
        }

        /// <summary>
        /// Finds the first durable item across pockets and backpack and wears
        /// down its durability by one use, so the low-durability color and the
        /// broken-item removal can be tested.
        /// </summary>
        private void ReduceFirstDurableItem()
        {
            if (ReduceInInventory(playerInventory.Pockets))
            {
                return;
            }

            if (playerInventory.Backpack != null
                && ReduceInInventory(playerInventory.Backpack))
            {
                return;
            }

            Debug.Log("No durable item found in the inventory.");
        }

        private bool ReduceInInventory(InventorySystem inventory)
        {
            for (int i = 0; i < inventory.SlotCount; i++)
            {
                ItemStack stack = inventory.GetSlot(i);
                if (stack == null || !stack.ItemData.IsDurable)
                {
                    continue;
                }

                bool broke = stack.ReduceDurability();
                Debug.Log($"Durability now {stack.CurrentDurability}"
                    + $"/{stack.ItemData.MaxDurability}"
                    + (broke ? " — item broke!" : string.Empty));

                if (broke)
                {
                    inventory.RemoveItem(stack.ItemData, stack.Quantity);
                }
                else
                {
                    inventory.NotifyChanged();
                }

                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Small helper: sets GUI.enabled for the duration of a using-block, then
    /// restores it. Lets a button be greyed out without leaking the state.
    /// </summary>
    internal readonly struct GUIEnabledScope : System.IDisposable
    {
        private readonly bool previous;

        public GUIEnabledScope(bool enabled)
        {
            previous = GUI.enabled;
            GUI.enabled = enabled;
        }

        public void Dispose()
        {
            GUI.enabled = previous;
        }
    }
}