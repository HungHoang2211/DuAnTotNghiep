using UnityEngine;
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
        [SerializeField] private GameObject root;

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
            if (root != null) root.SetActive(current < threshold);
        }
    }
}