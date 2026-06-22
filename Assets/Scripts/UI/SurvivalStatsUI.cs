using UnityEngine;
using TMPro;
using SimpleSurvival.Stats;

namespace SimpleSurvival.UI
{
    public sealed class SurvivalStatsUI : MonoBehaviour
    {
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private TMP_Text hungerLabel;
        [SerializeField] private TMP_Text thirstLabel;

        private void Awake()
        {
            if (playerStats == null)
                playerStats = FindAnyObjectByType<PlayerStats>();
        }

        private void OnEnable()
        {
            if (playerStats == null) return;
            playerStats.OnHungerChanged += HandleHungerChanged;
            playerStats.OnThirstChanged += HandleThirstChanged;

            HandleHungerChanged(playerStats.Hunger, playerStats.MaxHunger);
            HandleThirstChanged(playerStats.Thirst, playerStats.MaxThirst);
        }

        private void OnDisable()
        {
            if (playerStats == null) return;
            playerStats.OnHungerChanged -= HandleHungerChanged;
            playerStats.OnThirstChanged -= HandleThirstChanged;
        }

        private void HandleHungerChanged(float current, float max)
        {
            if (hungerLabel != null)
                hungerLabel.text = ((int)current).ToString();
        }

        private void HandleThirstChanged(float current, float max)
        {
            if (thirstLabel != null)
                thirstLabel.text = ((int)current).ToString();
        }
    }
}