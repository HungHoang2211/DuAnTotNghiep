using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SimpleSurvival.Progression
{
    public sealed class LevelExpUI : MonoBehaviour
    {
        [SerializeField] private Image expFill;
        [SerializeField] private TMP_Text levelNumberText;
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
            HandleExpChanged(levelSystem.CurrentExp, levelSystem.ExpToNextLevel, levelSystem.CurrentLevel);
        }

        private void HandleExpChanged(int currentExp, int expToNextLevel, int level)
        {
            if (levelNumberText != null)
                levelNumberText.text = level.ToString();

            if (expFill != null)
                expFill.fillAmount = expToNextLevel > 0 ? (float)currentExp / expToNextLevel : 1f;
        }
    }
}