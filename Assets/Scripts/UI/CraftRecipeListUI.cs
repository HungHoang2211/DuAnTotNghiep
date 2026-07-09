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

        public void Populate(List<CraftingRecipeData> recipes, Action<CraftingRecipeData> onRecipeSelected)
        {
            Debug.Log($"[CraftRecipeListUI] Populate called, isPopulated={isPopulated}, recipes count={recipes.Count}");

            if (isPopulated) return;
            isPopulated = true;

            foreach (CraftingRecipeData recipe in recipes)
            {
                CraftRecipeCellUI cell = Instantiate(cellPrefab, contentParent);
                cell.Init(recipe, onRecipeSelected);
                spawnedCells.Add(cell);
            }

            Debug.Log($"[CraftRecipeListUI] Spawned {spawnedCells.Count} cells");

            RefreshCraftableIcons();
        }

        public void RefreshCraftableIcons()
        {
            foreach (CraftRecipeCellUI cell in spawnedCells)
                cell.SetCraftable(craftDialog.HasEnoughIngredients(cell.Recipe));
        }
    }
}