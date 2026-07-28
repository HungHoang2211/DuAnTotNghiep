using System.Collections;
using System.Collections.Generic;
using SimpleSurvival.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleSurvival.UI
{
    public sealed class CraftDescriptionPanelUI : MonoBehaviour
    {
        private const string NotEnoughIngredientsMessage = "Thiếu nguyên liệu";
        private const string InventoryFullMessage = "Túi đồ đầy";
        private const string CraftedMessageFormat = "{0} +1";

        [Header("Info")]
        [SerializeField] private Image resultIcon;
        [SerializeField] private TMP_Text headerText;
        [SerializeField] private TMP_Text descriptionText;

        [Header("Ingredients")]
        [SerializeField] private List<CraftIngredientSlotUI> ingredientSlots;

        [Header("Craft Button")]
        [SerializeField] private GameObject craftButtonRoot;
        [SerializeField] private Button craftButton;

        [Header("Progress Bar")]
        [SerializeField] private GameObject progressBarRoot;
        [SerializeField] private Image progressBarFill;
        [SerializeField] private float craftDuration = 1f;

        [Header("References")]
        [SerializeField] private CraftNotifyUI notifyUI;
        [SerializeField] private CraftDialog craftDialog;

        private CraftingRecipeData currentRecipe;

        private void Awake()
        {
            craftButton.onClick.AddListener(OnCraftButtonClicked);
        }

        private void OnDestroy()
        {
            craftButton.onClick.RemoveListener(OnCraftButtonClicked);
        }

        public void Show(CraftingRecipeData recipe)
        {
            currentRecipe = recipe;

            resultIcon.sprite = recipe.ResultItem.Icon;
            headerText.text = recipe.ResultItem.ItemName;
            descriptionText.text = recipe.ResultItem.Description;

            craftButtonRoot.SetActive(true);
            progressBarRoot.SetActive(false);

            RefreshIngredients();
            RefreshCraftButtonInteractable();
        }

        private void OnCraftButtonClicked()
        {
            if (!craftDialog.HasEnoughIngredients(currentRecipe))
            {
                notifyUI.Show(NotEnoughIngredientsMessage);
                return;
            }

            if (!craftDialog.HasSpaceForResult(currentRecipe))
            {
                notifyUI.Show(InventoryFullMessage);
                return;
            }

            StartCoroutine(CraftRoutine());
        }

        private IEnumerator CraftRoutine()
        {
            craftDialog.SetInteractable(false);
            craftButtonRoot.SetActive(false);
            progressBarRoot.SetActive(true);
            progressBarFill.fillAmount = 0f;

            float elapsed = 0f;
            while (elapsed < craftDuration)
            {
                elapsed += Time.deltaTime;
                progressBarFill.fillAmount = elapsed / craftDuration;
                yield return null;
            }
            progressBarFill.fillAmount = 1f;

            try
            {
                craftDialog.PerformCraft(currentRecipe);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CraftDescriptionPanelUI] PerformCraft threw: {e}");
            }

            notifyUI.Show(string.Format(CraftedMessageFormat, currentRecipe.ResultItem.ItemName));

            progressBarRoot.SetActive(false);
            craftButtonRoot.SetActive(true);
            craftDialog.SetInteractable(true);

            RefreshIngredients();
            RefreshCraftButtonInteractable();
            craftDialog.NotifyCraftCompleted();
        }

        private void RefreshIngredients()
        {
            IReadOnlyList<CraftingRecipeData.Ingredient> ingredients = currentRecipe.Ingredients;

            for (int i = 0; i < ingredientSlots.Count; i++)
            {
                if (i < ingredients.Count)
                {
                    CraftingRecipeData.Ingredient ingredient = ingredients[i];
                    int currentAmount = craftDialog.CountItem(ingredient.Item);
                    ingredientSlots[i].SetData(ingredient.Item, currentAmount, ingredient.Amount);
                }
                else
                {
                    ingredientSlots[i].Hide();
                }
            }
        }

        private void RefreshCraftButtonInteractable()
        {
            craftButton.interactable = craftDialog.HasEnoughIngredients(currentRecipe);
        }
    }
}