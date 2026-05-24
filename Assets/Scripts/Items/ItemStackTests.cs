using NUnit.Framework;
using SimpleSurvival.Items;
using UnityEngine;

namespace SimpleSurvival.Items.Tests
{
    /// <summary>
    /// EditMode tests for ItemStack. These cover the boundary conditions
    /// (overflow, capping, breaking, copy independence) that are easy to get
    /// wrong and that the inventory system depends on.
    ///
    /// Run from Unity: Window > General > Test Runner > EditMode > Run All.
    /// </summary>
    public sealed class ItemStackTests
    {
        private static ItemData CreateStackableItem(int maxStack)
        {
            ItemData item = ScriptableObject.CreateInstance<ItemData>();
            SerializedItemBuilder.Configure(item, isStackable: true, maxStack: maxStack,
                maxDurability: 0);
            return item;
        }

        private static ItemData CreateDurableTool(int maxDurability)
        {
            ItemData item = ScriptableObject.CreateInstance<ItemData>();
            SerializedItemBuilder.Configure(item, isStackable: false, maxStack: 1,
                maxDurability: maxDurability);
            return item;
        }

        [Test]
        public void NewStack_StartsAtFullDurability()
        {
            ItemData tool = CreateDurableTool(maxDurability: 50);

            ItemStack stack = new ItemStack(tool, 1);

            Assert.AreEqual(50, stack.CurrentDurability);
        }

        [Test]
        public void AddQuantity_WithinMaxStack_ReportsNoOverflow()
        {
            ItemStack stack = new ItemStack(CreateStackableItem(maxStack: 99), 10);

            int overflow = stack.AddQuantity(20);

            Assert.AreEqual(0, overflow);
        }

        [Test]
        public void AddQuantity_BeyondMaxStack_ReturnsOverflow()
        {
            ItemStack stack = new ItemStack(CreateStackableItem(maxStack: 99), 90);

            int overflow = stack.AddQuantity(20);

            Assert.AreEqual(11, overflow);
        }

        [Test]
        public void RemoveQuantity_MoreThanHeld_RemovesOnlyWhatExists()
        {
            ItemStack stack = new ItemStack(CreateStackableItem(maxStack: 99), 5);

            int removed = stack.RemoveQuantity(10);

            Assert.AreEqual(5, removed);
        }

        [Test]
        public void ReduceDurability_OnLastUse_ReportsBroken()
        {
            ItemStack stack = new ItemStack(CreateDurableTool(maxDurability: 1), 1);

            bool brokeThisUse = stack.ReduceDurability();

            Assert.IsTrue(brokeThisUse);
        }

        [Test]
        public void ReduceDurability_OnNonDurableItem_DoesNothing()
        {
            ItemStack stack = new ItemStack(CreateStackableItem(maxStack: 99), 1);

            bool brokeThisUse = stack.ReduceDurability();

            Assert.IsFalse(brokeThisUse);
        }

        [Test]
        public void Clone_ProducesIndependentDurability()
        {
            ItemStack original = new ItemStack(CreateDurableTool(maxDurability: 10), 1);
            ItemStack copy = original.Clone();

            copy.ReduceDurability();

            Assert.AreEqual(10, original.CurrentDurability);
        }
    }
}
