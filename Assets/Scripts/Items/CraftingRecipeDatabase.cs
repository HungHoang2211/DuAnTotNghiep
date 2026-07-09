using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.Items
{
    [CreateAssetMenu(menuName = "Simple Survival/Crafting Recipe Database", fileName = "CraftingRecipeDatabase")]
    public sealed class CraftingRecipeDatabase : ScriptableObject
    {
        [SerializeField] private List<CraftingRecipeData> recipes = new List<CraftingRecipeData>();

        public IReadOnlyList<CraftingRecipeData> Recipes => recipes;

        public void SetRecipes(IEnumerable<CraftingRecipeData> source)
        {
            recipes = new List<CraftingRecipeData>(source);
        }
    }
}