using System.Collections.Generic;
using SimpleSurvival.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleSurvival.Building
{
    public sealed class BuildUpgradeCostUi : MonoBehaviour
    {
        [System.Serializable]
        public sealed class Slot
        {
            public GameObject root;
            public Image icon;
            public TMP_Text amountText;
        }

        [SerializeField] private List<Slot> slots;
        [SerializeField] private Color enoughColor = Color.white;
        [SerializeField] private Color notEnoughColor = Color.red;

        public void Show(BuildingData nextTier, PlayerInventoryQueries inventoryQueries)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (i < nextTier.DirectCost.Count)
                {
                    BuildingData.Ingredient ingredient = nextTier.DirectCost[i];
                    int current = inventoryQueries.CountItem(ingredient.Item);

                    slots[i].root.SetActive(true);
                    slots[i].icon.sprite = ingredient.Item.Icon;
                    slots[i].amountText.text = ingredient.Amount.ToString();
                    slots[i].amountText.color = current >= ingredient.Amount ? enoughColor : notEnoughColor;
                }
                else
                {
                    slots[i].root.SetActive(false);
                }
            }
        }

        public void Hide()
        {
            foreach (Slot slot in slots)
                slot.root.SetActive(false);
        }
    }
}