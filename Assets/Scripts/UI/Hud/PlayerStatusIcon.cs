using UnityEngine;
using UnityEngine.UI;
using SimpleSurvival.Stats;

namespace SimpleSurvival.UI.Hud
{
    public sealed class PlayerStatusIcon : MonoBehaviour
    {
        public enum StatKind
        {
            Hunger,
            Thirst
        }

        [Header("Stat")]
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private StatKind statKind;
        [SerializeField] private float threshold = 20f;

        [Header("Visual")]
        [SerializeField] private Image background;
        [SerializeField] private GameObject icon;

        private void OnEnable()
        {
            if (playerStats == null) return;

            if (statKind == StatKind.Hunger)
                playerStats.OnHungerChanged += HandleChanged;
            else
                playerStats.OnThirstChanged += HandleChanged;

            float initial = statKind == StatKind.Hunger ? playerStats.Hunger : playerStats.Thirst;
            UpdateState(initial);
        }

        private void OnDisable()
        {
            if (playerStats == null) return;

            if (statKind == StatKind.Hunger)
                playerStats.OnHungerChanged -= HandleChanged;
            else
                playerStats.OnThirstChanged -= HandleChanged;
        }

        private void HandleChanged(float current, float max)
        {
            UpdateState(current);
        }

        private void UpdateState(float current)
        {
            bool visible = current < threshold;

            if (background != null) background.enabled = visible;
            if (icon != null) icon.SetActive(visible);
        }
    }
}