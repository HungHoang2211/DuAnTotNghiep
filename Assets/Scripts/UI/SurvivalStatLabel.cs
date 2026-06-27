using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SimpleSurvival.Stats;

namespace SimpleSurvival.UI
{
    public sealed class SurvivalStatLabel : MonoBehaviour
    {
        public enum StatType
        {
            Hunger,
            Thirst
        }

        [System.Serializable]
        public struct ColorGradient
        {
            [Tooltip("Lower threshold. Color applied when amount >= this value.")]
            public int Threshold;
            public Color Color;
        }

        [Header("Stat")]
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private StatType statType;

        [Header("References")]
        [SerializeField] private Animation statAnimation;
        [SerializeField] private TextMeshProUGUI amountText;
        [SerializeField] private Image iconStat;
        [SerializeField] private RectTransform cacheAmountTransform;

        [Header("Colors")]
        [Tooltip("Sort ascending by Threshold. Color applied when amount <= Threshold.")]
        [SerializeField] private ColorGradient[] colors;
        [SerializeField] private Color increaseColor = Color.green;

        [Header("Animation")]
        [SerializeField] private string animationClipName = "Stats_Low";

        private Color _currentTextColor;
        private Vector3 _defaultPosition;
        private Vector3 _defaultScale;
        private Quaternion _defaultRotation;
        private bool _isPlaying;
        private float _lastAmount;

        private void Awake()
        {
            InitCache();
            if (playerStats == null)
                playerStats = FindAnyObjectByType<PlayerStats>();
        }

        private void OnEnable()
        {
            if (playerStats == null) return;

            if (statType == StatType.Hunger)
                playerStats.OnHungerChanged += HandleChanged;
            else
                playerStats.OnThirstChanged += HandleChanged;

            float initial = GetCurrentAmount();
            _lastAmount = initial;
            SetAmount((int)initial, false);
        }

        private void OnDisable()
        {
            if (playerStats == null) return;

            if (statType == StatType.Hunger)
                playerStats.OnHungerChanged -= HandleChanged;
            else
                playerStats.OnThirstChanged -= HandleChanged;
        }

        private void InitCache()
        {
            if (cacheAmountTransform == null) return;
            _defaultPosition = cacheAmountTransform.localPosition;
            _defaultScale = cacheAmountTransform.localScale;
            _defaultRotation = cacheAmountTransform.localRotation;
        }

        private float GetCurrentAmount()
        {
            if (playerStats == null) return 0f;
            return statType == StatType.Hunger ? playerStats.Hunger : playerStats.Thirst;
        }

        private void HandleChanged(float current, float max)
        {
            int displayCurrent = Mathf.CeilToInt(current);
            int displayLast = Mathf.CeilToInt(_lastAmount);
            bool isIncrease = displayCurrent > displayLast;
            _lastAmount = current;
            SetAmount(displayCurrent, isIncrease);
        }

        private void SetAmount(int amount, bool isIncrease)
        {
            if (amountText == null) return;

            amountText.text = amount.ToString();
            _currentTextColor = GetColor(amount);

            if (!_isPlaying)
                amountText.color = _currentTextColor;

            if (gameObject.activeInHierarchy && isIncrease)
                StartCoroutine(ShowAnimation());
        }

        private IEnumerator ShowAnimation()
        {
            Play();
            amountText.color = increaseColor;
            _isPlaying = true;

            while (statAnimation != null && statAnimation.isPlaying)
                yield return new WaitForEndOfFrame();

            _isPlaying = false;
            amountText.color = _currentTextColor;
        }

        private void Play()
        {
            if (statAnimation == null) return;
            statAnimation.Play(animationClipName);
            _isPlaying = true;
        }

        private void Stop()
        {
            if (!_isPlaying || cacheAmountTransform == null) return;

            cacheAmountTransform.localPosition = _defaultPosition;
            cacheAmountTransform.localScale = _defaultScale;
            cacheAmountTransform.localRotation = _defaultRotation;

            if (statAnimation != null)
                statAnimation.Stop();

            _isPlaying = false;
        }

        private Color GetColor(int amount)
        {
            Color result = Color.white;
            if (colors == null) return result;

            foreach (ColorGradient grad in colors)
            {
                if (grad.Threshold <= amount)
                {
                    result = grad.Color;
                    continue;
                }
                break;
            }
            return result;
        }
    }
}