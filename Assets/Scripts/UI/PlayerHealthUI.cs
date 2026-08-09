using UnityEngine;
using TMPro;
using SimpleSurvival.Stats;

namespace SimpleSurvival.UI.HealthBar
{
    public sealed class PlayerHealthUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerStats playerStats;

        [Header("Main HP Bar (3 layers)")]
        [SerializeField] private BarAnimation hpBar;

        [Header("Labels")]
        [SerializeField] private TMP_Text hpAmountLabel;
        [SerializeField] private TMP_Text playerNameLabel;
        [SerializeField] private string playerName = "Player";

        private void Awake()
        {
            if (playerStats == null)
                playerStats = FindAnyObjectByType<PlayerStats>();
        }

        private void OnEnable()
        {
            if (playerStats != null)
                playerStats.OnHPChanged += HandleHPChanged;
            if (playerNameLabel != null)
                playerNameLabel.text = playerName;

            InitializeBars();
        }

        private void OnDisable()
        {
            if (playerStats != null)
                playerStats.OnHPChanged -= HandleHPChanged;
        }

        private void InitializeBars()
        {
            if (playerStats == null) return;
            float percent = playerStats.HP / playerStats.MaxHP;
            if (hpBar != null) hpBar.SetValue(percent);
            UpdateLabel();
        }

        private void HandleHPChanged(float current, float max)
        {
            float percent = current / max;
            if (hpBar != null) hpBar.AnimateValue(percent);
            UpdateLabel();
        }

        private void UpdateLabel()
        {
            if (hpAmountLabel == null || playerStats == null) return;
            hpAmountLabel.text = ((int)playerStats.HP).ToString();
        }
    }
}