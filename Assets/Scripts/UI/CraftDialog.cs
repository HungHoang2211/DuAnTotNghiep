using SimpleSurvival.Items;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleSurvival.UI
{
    public sealed class CraftDialog : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject root;
        [SerializeField] private CanvasGroup rootCanvasGroup;

        [Header("Buttons")]
        [SerializeField] private Button hudCraftButton;
        [SerializeField] private Button closeButton;

        [Header("Panels")]
        [SerializeField] private CraftRecipeListUI recipeList;
        [SerializeField] private CraftDescriptionPanelUI descriptionPanel;

        [Header("Data")]
        [SerializeField] private CraftingRecipeDatabase recipeDatabase;
        [SerializeField] private PlayerInventoryQueries inventoryQueries;

        public bool IsOpen => root != null && root.activeSelf;

        private void Awake()
        {
            if (root != null) root.SetActive(false);
            if (hudCraftButton != null) hudCraftButton.onClick.AddListener(Open);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        private void OnDestroy()
        {
            if (hudCraftButton != null) hudCraftButton.onClick.RemoveListener(Open);
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        }

        public void Open()
        {
            InventoryPanelController.Instance?.Close();

            root.SetActive(true);
            recipeList.Populate(recipeDatabase.Recipes, OnRecipeSelected);

            CraftingRecipeData firstRecipe = recipeList.FirstRecipe;
            if (firstRecipe != null)
                OnRecipeSelected(firstRecipe);
        }

        public void Close()
        {
            root.SetActive(false);
        }

        public void SetInteractable(bool interactable)
        {
            rootCanvasGroup.interactable = interactable;
        }

        public void NotifyCraftCompleted()
        {
            recipeList.RefreshCraftableIcons();
        }

        public bool HasEnoughIngredients(CraftingRecipeData recipe)
        {
            foreach (CraftingRecipeData.Ingredient ingredient in recipe.Ingredients)
            {
                if (inventoryQueries.CountItem(ingredient.Item) < ingredient.Amount)
                    return false;
            }
            return true;
        }

        public bool HasSpaceForResult(CraftingRecipeData recipe)
        {
            return inventoryQueries.CanAddItem(recipe.ResultItem, 1);
        }

        public int CountItem(ItemData itemData)
        {
            return inventoryQueries.CountItem(itemData);
        }

        public void PerformCraft(CraftingRecipeData recipe)
        {
            foreach (CraftingRecipeData.Ingredient ingredient in recipe.Ingredients)
                inventoryQueries.RemoveItemAmount(ingredient.Item, ingredient.Amount);

            inventoryQueries.AddItem(recipe.ResultItem, 1);
        }

        private void OnRecipeSelected(CraftingRecipeData recipe)
        {
            descriptionPanel.Show(recipe);
        }
    }
}