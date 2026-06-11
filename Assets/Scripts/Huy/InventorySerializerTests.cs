using NUnit.Framework;
using SimpleSurvival.Items;
using UnityEditor;
using UnityEngine;

namespace SimpleSurvival.SaveLoad.Tests
{
    public sealed class InventorySerializerTests
    {
        [Test]
        public void RoundTrip_PreservesItemAtSlot()
        {
            ItemData wood = CreateItem("res_wood", stackable: true, maxStack: 99, maxDurability: 0);
            ItemDatabase database = CreateDatabase(wood);
            InventorySystem source = new InventorySystem(5);
            source.SetSlot(2, new ItemStack(wood, 10));

            InventorySystem restored = RoundTrip(source, database);

            Assert.AreEqual(wood, restored.GetSlot(2).ItemData);
        }

        [Test]
        public void RoundTrip_PreservesQuantity()
        {
            ItemData wood = CreateItem("res_wood", stackable: true, maxStack: 99, maxDurability: 0);
            ItemDatabase database = CreateDatabase(wood);
            InventorySystem source = new InventorySystem(5);
            source.SetSlot(0, new ItemStack(wood, 42));

            InventorySystem restored = RoundTrip(source, database);

            Assert.AreEqual(42, restored.GetSlot(0).Quantity);
        }

        [Test]
        public void RoundTrip_PreservesPartialDurability()
        {
            ItemData axe = CreateItem("tool_axe", stackable: false, maxStack: 1, maxDurability: 50);
            ItemDatabase database = CreateDatabase(axe);
            ItemStack used = new ItemStack(axe, 1);
            used.ReduceDurability();
            used.ReduceDurability();
            InventorySystem source = new InventorySystem(5);
            source.SetSlot(1, used);

            InventorySystem restored = RoundTrip(source, database);

            Assert.AreEqual(48, restored.GetSlot(1).CurrentDurability);
        }

        [Test]
        public void RoundTrip_KeepsEmptySlotEmpty()
        {
            ItemData wood = CreateItem("res_wood", stackable: true, maxStack: 99, maxDurability: 0);
            ItemDatabase database = CreateDatabase(wood);
            InventorySystem source = new InventorySystem(5);
            source.SetSlot(0, new ItemStack(wood, 1));

            InventorySystem restored = RoundTrip(source, database);

            Assert.IsNull(restored.GetSlot(3));
        }

        [Test]
        public void RoundTrip_KeepsSlotPosition()
        {
            ItemData wood = CreateItem("res_wood", stackable: true, maxStack: 99, maxDurability: 0);
            ItemDatabase database = CreateDatabase(wood);
            InventorySystem source = new InventorySystem(5);
            source.SetSlot(4, new ItemStack(wood, 1));

            InventorySystem restored = RoundTrip(source, database);

            Assert.IsNotNull(restored.GetSlot(4));
        }

        [Test]
        public void Restore_DropsUnknownItemId()
        {
            ItemDatabase database = CreateDatabase();
            InventorySerializer serializer = new InventorySerializer(database);
            InventoryData data = new InventoryData { slotCount = 5 };
            data.stacks.Add(new ItemStackData { slot = 0, itemId = "ghost", quantity = 1, durability = 0 });
            InventorySystem restored = new InventorySystem(5);

            serializer.Restore(data, restored);

            Assert.IsNull(restored.GetSlot(0));
        }

        [Test]
        public void Restore_DropsOutOfRangeSlot()
        {
            ItemData wood = CreateItem("res_wood", stackable: true, maxStack: 99, maxDurability: 0);
            ItemDatabase database = CreateDatabase(wood);
            InventorySerializer serializer = new InventorySerializer(database);
            InventoryData data = new InventoryData { slotCount = 3 };
            data.stacks.Add(new ItemStackData { slot = 7, itemId = "res_wood", quantity = 1, durability = 0 });
            InventorySystem restored = new InventorySystem(3);

            serializer.Restore(data, restored);

            Assert.IsNull(restored.GetSlot(0));
        }

        [Test]
        public void Capture_SkipsItemWithoutId()
        {
            ItemData noId = CreateItem("", stackable: true, maxStack: 99, maxDurability: 0);
            InventorySerializer serializer = new InventorySerializer(CreateDatabase());
            InventorySystem source = new InventorySystem(5);
            source.SetSlot(0, new ItemStack(noId, 1));

            InventoryData data = serializer.Capture(source);

            Assert.AreEqual(0, data.stacks.Count);
        }

        private static InventorySystem RoundTrip(InventorySystem source, ItemDatabase database)
        {
            InventorySerializer serializer = new InventorySerializer(database);
            InventoryData data = serializer.Capture(source);

            InventorySystem restored = new InventorySystem(source.SlotCount);
            serializer.Restore(data, restored);
            return restored;
        }

        private static ItemDatabase CreateDatabase(params ItemData[] items)
        {
            ItemDatabase database = ScriptableObject.CreateInstance<ItemDatabase>();
            database.SetItems(items);
            return database;
        }

        private static ItemData CreateItem(string itemId, bool stackable, int maxStack, int maxDurability)
        {
            ItemData item = ScriptableObject.CreateInstance<ItemData>();

            SerializedObject serialized = new SerializedObject(item);
            serialized.FindProperty("itemId").stringValue = itemId;
            serialized.FindProperty("itemName").stringValue = itemId;
            serialized.FindProperty("isStackable").boolValue = stackable;
            serialized.FindProperty("maxStack").intValue = maxStack;
            serialized.FindProperty("maxDurability").intValue = maxDurability;
            serialized.ApplyModifiedProperties();

            return item;
        }
    }
}