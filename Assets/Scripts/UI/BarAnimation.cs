using UnityEngine;
using UnityEngine.UI;

namespace SimpleSurvival.UI.HealthBar
{
    public class BarAnimation : MonoBehaviour
    {
        [Header("Bars")]
        [SerializeField] private Image bar;
        [SerializeField] private Image barBack;

        [Header("Colors")]
        [SerializeField] private Color damageColor = new Color(0.85f, 0.3f, 0.3f);
        [SerializeField] private Color healColor = new Color(0.5f, 0.85f, 0.5f);

        [Header("Animation")]
        [SerializeField] private float speed = 3f;
        [SerializeField] private float speedLimitMin = 0.1f;
        [SerializeField] private float speedLimitMax = 2f;

        private static readonly Color WhiteColor = Color.white;

        private float _currentPercent;
        private float _currentBlinkLevel;
        private bool _isAnimate;
        private Color _barBackColor;

        public void SetValue(float value)
        {
            _isAnimate = false;
            _currentPercent = Mathf.Clamp01(value);
            if (bar != null) bar.fillAmount = _currentPercent;
            if (barBack != null) barBack.fillAmount = _currentPercent;
        }

        public void AnimateValue(float value)
        {
            _currentPercent = Mathf.Clamp01(value);
            if (!_isAnimate)
            {
                _isAnimate = true;
                _currentBlinkLevel = 1f;
            }
        }

        public void SetBarColor(Color color)
        {
            if (bar != null) bar.color = color;
        }

        private void Update()
        {
            if (!_isAnimate || bar == null || barBack == null) return;

            float fillAmount = bar.fillAmount;
            float fillAmountBack = barBack.fillAmount;

            if (fillAmount < _currentPercent)
            {
                barBack.fillAmount = _currentPercent;
                _barBackColor = healColor;
                fillAmount = Mathf.MoveTowards(fillAmount, _currentPercent, GetSpeed(fillAmount));
                bar.fillAmount = fillAmount;

                if (Mathf.Approximately(fillAmount, _currentPercent))
                    _isAnimate = false;
            }
            else if (fillAmountBack > _currentPercent)
            {
                bar.fillAmount = _currentPercent;
                _barBackColor = damageColor;
                fillAmountBack = Mathf.MoveTowards(fillAmountBack, _currentPercent, GetSpeed(fillAmountBack));
                barBack.fillAmount = fillAmountBack;

                if (Mathf.Approximately(fillAmountBack, _currentPercent))
                    _isAnimate = false;
            }

            UpdateBlink();
        }

        private void UpdateBlink()
        {
            if (_currentBlinkLevel > 0f)
            {
                _currentBlinkLevel = Mathf.MoveTowards(_currentBlinkLevel, 0f, Time.deltaTime);
                Color color = Color.Lerp(_barBackColor, WhiteColor, _currentBlinkLevel);
                barBack.color = color;
            }
            else
            {
                barBack.color = _barBackColor;
            }
        }

        private float GetSpeed(float pos)
        {
            return Time.deltaTime * Mathf.Clamp(Mathf.Abs(pos - _currentPercent) * speed, speedLimitMin, speedLimitMax);
        }
    }
}