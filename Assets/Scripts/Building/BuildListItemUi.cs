using SimpleSurvival.Audio;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleSurvival.Building
{
    public sealed class BuildListItemUi : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image costIconImage;
        [SerializeField] private TMP_Text costAmountText;
        [SerializeField] private CanvasGroup highlightCanvasGroup;

        public BuildingData Building { get; private set; }

        public void Init(BuildingData building, Action<BuildListItemUi> onClicked)
        {
            Building = building;
            iconImage.sprite = building.Icon;

            if (building.DirectCost.Count > 0)
            {
                BuildingData.Ingredient firstIngredient = building.DirectCost[0];
                costIconImage.enabled = true;
                costIconImage.sprite = firstIngredient.Item.Icon;
                costAmountText.enabled = true;
                costAmountText.text = firstIngredient.Amount.ToString();
            }
            else
            {
                costIconImage.enabled = false;
                costAmountText.enabled = false;
            }

            SetSelected(false);
            button.onClick.AddListener(() => onClicked(this));
        }

        public void SetSelected(bool selected)
        {
            highlightCanvasGroup.alpha = selected ? 1f : 0f;
            UIAudioController.Instance.PlayMainClick();
        }
    }
}