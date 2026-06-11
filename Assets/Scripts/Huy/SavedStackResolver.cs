using SimpleSurvival.Items;
using UnityEngine;

namespace SimpleSurvival.SaveLoad
{
    public sealed class SavedStackResolver
    {
        private readonly ItemDatabase database;

        public SavedStackResolver(ItemDatabase database)
        {
            this.database = database;
        }

        public bool TryResolve(string itemId, int quantity, int durability, out ItemStack stack)
        {
            stack = null;

            if (quantity < 1)
            {
                Debug.LogWarning($"Dropping saved stack '{itemId}': quantity {quantity} is invalid.");
                return false;
            }

            if (!database.TryGet(itemId, out ItemData item))
            {
                Debug.LogWarning($"Dropping saved stack: unknown itemId '{itemId}'.");
                return false;
            }

            stack = new ItemStack(item, quantity, durability);
            return true;
        }
    }
}