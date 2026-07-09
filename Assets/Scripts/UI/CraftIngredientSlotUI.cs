using SimpleSurvival.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleSurvival.UI
{
    public sealed class CraftIngredientSlotUI : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text needCountText;
        [SerializeField] private TMP_Text currentCountText;
        [SerializeField] private Color enoughColor = Color.white;
        [SerializeField] private Color notEnoughColor = Color.red;

        public void SetData(ItemData item, int currentAmount, int neededAmount)
        {
            gameObject.SetActive(true);

            icon.sprite = item.Icon;
            needCountText.text = currentAmount.ToString();
            needCountText.color = currentAmount >= neededAmount ? enoughColor : notEnoughColor;
            currentCountText.text = "/" + neededAmount;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}