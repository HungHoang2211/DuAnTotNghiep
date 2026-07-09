using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.Items
{
    [CreateAssetMenu(menuName = "Simple Survival/Crafting Recipe", fileName = "NewCraftingRecipe")]
    public sealed class CraftingRecipeData : ScriptableObject
    {
        public const int MaxIngredients = 6;

        [System.Serializable]
        public sealed class Ingredient
        {
            [SerializeField] private ItemData item;
            [SerializeField] private int amount = 1;

            public ItemData Item => item;
            public int Amount => amount;
        }

        [SerializeField] private ItemData resultItem;
        [SerializeField] private List<Ingredient> ingredients = new List<Ingredient>();

        public ItemData ResultItem => resultItem;
        public IReadOnlyList<Ingredient> Ingredients => ingredients;

        private void OnValidate()
        {
            if (ingredients.Count > MaxIngredients)
                ingredients.RemoveRange(MaxIngredients, ingredients.Count - MaxIngredients);
        }
    }
}
