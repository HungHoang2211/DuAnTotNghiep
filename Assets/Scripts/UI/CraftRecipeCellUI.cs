using System;
using SimpleSurvival.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleSurvival.UI
{
    public sealed class CraftRecipeCellUI : MonoBehaviour
    {
        [SerializeField] private Button backgroundButton;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private GameObject craftableIcon;

        public CraftingRecipeData Recipe { get; private set; }

        public void Init(CraftingRecipeData recipe, Action<CraftingRecipeData> onClicked)
        {
            Recipe = recipe;
            iconImage.sprite = recipe.ResultItem.Icon;
            nameText.text = recipe.ResultItem.ItemName;
            backgroundButton.onClick.AddListener(() => onClicked(Recipe));
        }

        public void SetCraftable(bool canCraft)
        {
            craftableIcon.SetActive(canCraft);
        }
    }
}