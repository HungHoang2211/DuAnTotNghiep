using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SimpleSurvival.Stats;

namespace SimpleSurvival.UI.HealthBar
{
    public class PlayerHealthUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerStats playerStats;

        [Header("Main HP Bar (Bar + Bar_Back)")]
        [SerializeField] private BarAnimation hpBar;

        [Header("Heal Over Time Preview")]
        [SerializeField] private Image healOverTimeBar;
        [SerializeField] private float healPreviewSeconds = 5f;

        [Header("Labels")]
        [SerializeField] private TMP_Text hpAmountLabel;
        [SerializeField] private TMP_Text playerNameLabel;
        [SerializeField] private string playerName = "Player";

        private void Start()
        {

            InitializeBars();
        }

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
        }

        private void OnDisable()
        {
            if (playerStats != null)
                playerStats.OnHPChanged -= HandleHPChanged;
        }

        private void Update()
        {
            UpdateHealOverTimePreview();
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

        private void UpdateHealOverTimePreview()
        {
            if (healOverTimeBar == null || playerStats == null) return;

            float regenPerSec = EstimateRegenPerSec();
            float predictedHP = Mathf.Min(playerStats.HP + regenPerSec * healPreviewSeconds, playerStats.MaxHP);
            float percent = predictedHP / playerStats.MaxHP;

            healOverTimeBar.fillAmount = percent;
        }

        private float EstimateRegenPerSec()
        {
            return 2f;
        }
    }
}