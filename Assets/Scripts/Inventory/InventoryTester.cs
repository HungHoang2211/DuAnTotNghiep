using System.Collections.Generic;
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
        [Tooltip("Stackable items to test with, e.g. wood, stone.")]
        [SerializeField] private List<ItemData> stackableItems = new List<ItemData>();

        [Tooltip("Durable items to test with, e.g. axe, helmet.")]
        [SerializeField] private List<ItemData> durableItems = new List<ItemData>();

        [Tooltip("Single-slot items without stacking or durability, e.g. backpack, key.")]
        [SerializeField] private List<ItemData> miscItems = new List<ItemData>();

        [Header("Settings")]
        [SerializeField] private int addAmount = 5;
        [SerializeField] private int backpackStep = 5;

        [Header("Random Durability")]
        [Tooltip("Minimum durability ratio when spawning loot items (0 = broken, 1 = full).")]
        [SerializeField, Range(0f, 1f)] private float minDurabilityRatio = 0.3f;

        [Tooltip("Maximum durability ratio when spawning loot items.")]
        [SerializeField, Range(0f, 1f)] private float maxDurabilityRatio = 0.9f;

        private int currentBackpackSlots;
        private int selectedStackableIndex;
        private int selectedDurableIndex;
        private int selectedMiscIndex;

        private void Awake()
        {
            currentBackpackSlots = 0;
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 280, 600), GUI.skin.box);
            GUILayout.Label("INVENTORY TESTER");

            DrawStackableItemButtons();
            GUILayout.Space(8);
            DrawDurableItemButtons();
            GUILayout.Space(8);
            DrawMiscItemButtons();
            GUILayout.Space(8);
            DrawBackpackButtons();
            GUILayout.Space(8);
            DrawDurabilityButton();

            GUILayout.EndArea();
        }

        // ── Stackable ────────────────────────────────────────────────────────

        private void DrawStackableItemButtons()
        {
            GUILayout.Label("Stackable Items");

            if (stackableItems.Count == 0)
            {
                GUILayout.Label("  (no stackable items assigned)");
                return;
            }

            DrawItemSelector(ref selectedStackableIndex, stackableItems);

            ItemData selected = stackableItems[selectedStackableIndex];

            if (GUILayout.Button($"Add {addAmount} '{selected.ItemName}' → Pockets"))
                AddToPockets(selected, addAmount);

            if (GUILayout.Button($"Add {addAmount} '{selected.ItemName}' → Backpack"))
                AddToBackpack(selected, addAmount);
        }

        // ── Durable ──────────────────────────────────────────────────────────

        private void DrawDurableItemButtons()
        {
            GUILayout.Label("Durable Items");

            if (durableItems.Count == 0)
            {
                GUILayout.Label("  (no durable items assigned)");
                return;
            }

            DrawItemSelector(ref selectedDurableIndex, durableItems);

            ItemData selected = durableItems[selectedDurableIndex];

            if (GUILayout.Button($"Add 1 '{selected.ItemName}' (full dur.) → Pockets"))
                AddToPockets(selected, 1);

            if (GUILayout.Button($"Add 1 '{selected.ItemName}' (random dur.) → Pockets"))
                AddToPocketsWithRandomDurability(selected);
        }

        // ── Misc ─────────────────────────────────────────────────────────────

        private void DrawMiscItemButtons()
        {
            GUILayout.Label("Misc Items");

            if (miscItems.Count == 0)
            {
                GUILayout.Label("  (no misc items assigned)");
                return;
            }

            DrawItemSelector(ref selectedMiscIndex, miscItems);

            ItemData selected = miscItems[selectedMiscIndex];

            if (GUILayout.Button($"Add 1 '{selected.ItemName}' → Pockets"))
                AddToPockets(selected, 1);
        }

        // ── Backpack ─────────────────────────────────────────────────────────

        private void DrawBackpackButtons()
        {
            int max = playerInventory.MaxBackpackSlotCount;
            GUILayout.Label($"Backpack slots: {currentBackpackSlots} / {max}");

            using (new GUIEnabledScope(currentBackpackSlots < max))
            {
                if (GUILayout.Button($"+{backpackStep} backpack slots"))
                    ResizeBackpack(currentBackpackSlots + backpackStep);
            }

            using (new GUIEnabledScope(currentBackpackSlots > 0))
            {
                if (GUILayout.Button($"-{backpackStep} backpack slots"))
                    ResizeBackpack(currentBackpackSlots - backpackStep);
            }
        }

        // ── Durability ───────────────────────────────────────────────────────

        private void DrawDurabilityButton()
        {
            GUILayout.Label("Durability");

            if (GUILayout.Button("Reduce durability of first durable item"))
                ReduceFirstDurableItem();
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        /// <summary>Draws prev/next arrows to cycle through an item list.</summary>
        private void DrawItemSelector(ref int index, List<ItemData> items)
        {
            GUILayout.BeginHorizontal();

            using (new GUIEnabledScope(index > 0))
            {
                if (GUILayout.Button("◀", GUILayout.Width(30)))
                    index--;
            }

            GUILayout.Label(items[index].ItemName, GUI.skin.box,
                GUILayout.ExpandWidth(true));

            using (new GUIEnabledScope(index < items.Count - 1))
            {
                if (GUILayout.Button("▶", GUILayout.Width(30)))
                    index++;
            }

            GUILayout.EndHorizontal();
        }

        private void AddToPockets(ItemData item, int amount)
        {
            int overflow = playerInventory.Pockets.AddItem(item, amount);
            LogOverflow("Pockets", overflow);
        }

        private void AddToBackpack(ItemData item, int amount)
        {
            if (playerInventory.Backpack == null)
            {
                Debug.Log("No backpack equipped — add backpack slots first.");
                return;
            }

            int overflow = playerInventory.Backpack.AddItem(item, amount);
            LogOverflow("Backpack", overflow);
        }

        private void AddToPocketsWithRandomDurability(ItemData item)
        {
            int randomDurability = Mathf.RoundToInt(
                Random.Range(minDurabilityRatio, maxDurabilityRatio) * item.MaxDurability
            );

            ItemStack stack = new ItemStack(item, 1, randomDurability);
            int overflow = playerInventory.Pockets.AddStack(stack);

            Debug.Log($"Added '{item.ItemName}' with durability "
                + $"{randomDurability}/{item.MaxDurability}.");
            LogOverflow("Pockets", overflow);
        }

        private void ResizeBackpack(int newSlotCount)
        {
            int overflow = playerInventory.ResizeBackpack(newSlotCount);
            currentBackpackSlots = Mathf.Clamp(newSlotCount, 0,
                playerInventory.MaxBackpackSlotCount);

            if (overflow > 0)
            {
                Debug.Log($"Backpack shrunk — {overflow} item(s) lost.");
            }
        }

        private void ReduceFirstDurableItem()
        {
            if (ReduceInInventory(playerInventory.Pockets))
                return;

            if (playerInventory.Backpack != null
                && ReduceInInventory(playerInventory.Backpack))
                return;

            Debug.Log("No durable item found in the inventory.");
        }

        private bool ReduceInInventory(InventorySystem inventory)
        {
            for (int i = 0; i < inventory.SlotCount; i++)
            {
                ItemStack stack = inventory.GetSlot(i);
                if (stack == null || !stack.ItemData.IsDurable)
                    continue;

                bool broke = stack.ReduceDurability();
                Debug.Log($"Durability now {stack.CurrentDurability}"
                    + $"/{stack.ItemData.MaxDurability}"
                    + (broke ? " — item broke!" : string.Empty));

                if (broke)
                    inventory.RemoveItem(stack.ItemData, stack.Quantity);
                else
                    inventory.NotifyChanged();

                return true;
            }

            return false;
        }

        private static void LogOverflow(string target, int overflow)
        {
            if (overflow > 0)
                Debug.Log($"{target} full — {overflow} item(s) did not fit.");
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