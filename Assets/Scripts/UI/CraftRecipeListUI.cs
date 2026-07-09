using System;
using System.Collections.Generic;
using SimpleSurvival.Items;
using UnityEngine;

namespace SimpleSurvival.UI
{
    public sealed class CraftRecipeListUI : MonoBehaviour
    {
        [SerializeField] private CraftRecipeCellUI cellPrefab;
        [SerializeField] private Transform contentParent;
        [SerializeField] private CraftDialog craftDialog;

        private readonly List<CraftRecipeCellUI> spawnedCells = new List<CraftRecipeCellUI>();
        private bool isPopulated;

        public CraftingRecipeData FirstRecipe =>
            spawnedCells.Count > 0 ? spawnedCells[0].Recipe : null;

        public void Populate(IReadOnlyList<CraftingRecipeData> recipes, Action<CraftingRecipeData> onRecipeSelected)
        {
            if (isPopulated) return;
            isPopulated = true;

            foreach (CraftingRecipeData recipe in recipes)
            {
                CraftRecipeCellUI cell = Instantiate(cellPrefab, contentParent);
                cell.Init(recipe, onRecipeSelected);
                spawnedCells.Add(cell);
            }

            RefreshCraftableIcons();
        }

        public void RefreshCraftableIcons()
        {
            foreach (CraftRecipeCellUI cell in spawnedCells)
                cell.SetCraftable(craftDialog.HasEnoughIngredients(cell.Recipe));
        }
    }
}