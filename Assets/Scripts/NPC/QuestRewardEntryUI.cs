using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SimpleSurvival.Items;

namespace SimpleSurvival.Quests
{
    public sealed class QuestRewardEntryUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text quantityText;

        public void SetReward(ItemData itemData, int quantity)
        {
            if (iconImage != null && itemData != null)
                iconImage.sprite = itemData.Icon;

            if (quantityText != null)
                quantityText.text = $"x{quantity}";
        }
    }
}