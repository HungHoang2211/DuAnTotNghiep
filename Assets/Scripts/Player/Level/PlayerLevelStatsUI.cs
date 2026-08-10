using TMPro;
using UnityEngine;
using SimpleSurvival.Progression;

namespace SimpleSurvival.UI
{
    public sealed class PlayerLevelStatsUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private PlayerLevelSystem levelSystem;

        private void OnEnable()
        {
            if (levelSystem == null) return;
            levelSystem.OnExpChanged += HandleExpChanged;
            Refresh();
        }

        private void OnDisable()
        {
            if (levelSystem == null) return;
            levelSystem.OnExpChanged -= HandleExpChanged;
        }

        private void Refresh()
        {
            if (levelText != null)
                levelText.text = levelSystem.CurrentLevel.ToString();
        }

        private void HandleExpChanged(int currentExp, int expToNextLevel, int level)
        {
            if (levelText != null)
                levelText.text = level.ToString();
        }
    }
}