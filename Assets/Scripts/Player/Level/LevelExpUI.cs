using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SimpleSurvival.Progression
{
    public sealed class LevelExpUI : MonoBehaviour
    {
        private const string ExpGainedFormat = "+{0}xp";

        [SerializeField] private Image expFill;
        [SerializeField] private TMP_Text levelNumberText;
        [SerializeField] private PlayerLevelSystem levelSystem;

        [Header("XP Gain Popup")]
        [SerializeField] private TMP_Text xpGainLabel;
        [SerializeField] private float fadeInDuration = 0.15f;
        [SerializeField] private float holdDuration = 0.8f;
        [SerializeField] private float fadeOutDuration = 0.4f;

        private Coroutine xpGainRoutine;

        private void Awake()
        {
            if (xpGainLabel != null)
                SetLabelAlpha(0f);
        }

        private void OnEnable()
        {
            if (levelSystem == null) return;
            levelSystem.OnExpChanged += HandleExpChanged;
            levelSystem.OnExpGained += HandleExpGained;
            Refresh();
        }

        private void OnDisable()
        {
            if (levelSystem == null) return;
            levelSystem.OnExpChanged -= HandleExpChanged;
            levelSystem.OnExpGained -= HandleExpGained;
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

        private void HandleExpGained(int amount)
        {
            if (xpGainLabel == null) return;

            xpGainLabel.text = string.Format(ExpGainedFormat, amount);

            if (xpGainRoutine != null)
                StopCoroutine(xpGainRoutine);

            xpGainRoutine = StartCoroutine(XpGainRoutine());
        }

        private IEnumerator XpGainRoutine()
        {
            yield return FadeLabel(0f, 1f, fadeInDuration);
            yield return new WaitForSeconds(holdDuration);
            yield return FadeLabel(1f, 0f, fadeOutDuration);

            xpGainRoutine = null;
        }

        private IEnumerator FadeLabel(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                SetLabelAlpha(Mathf.Lerp(from, to, elapsed / duration));
                yield return null;
            }
            SetLabelAlpha(to);
        }

        private void SetLabelAlpha(float alpha)
        {
            Color color = xpGainLabel.color;
            color.a = alpha;
            xpGainLabel.color = color;
        }
    }
}