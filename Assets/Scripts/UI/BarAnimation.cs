using UnityEngine;
using UnityEngine.UI;

namespace SimpleSurvival.UI.HealthBar
{
    public sealed class BarAnimation : MonoBehaviour
    {
        [Header("Bars")]
        [Tooltip("Front bar showing current value (e.g. green HP).")]
        [SerializeField] private Image bar;
        [Tooltip("Back bar showing damage trail (e.g. red).")]
        [SerializeField] private Image barBack;
        [Tooltip("Optional back bar showing heal trail (e.g. green light). Leave null if not used.")]
        [SerializeField] private Image healTrail;

        [Header("Animation")]
        [SerializeField] private float speed = 3f;
        [SerializeField] private float speedLimitMin = 0.1f;
        [SerializeField] private float speedLimitMax = 2f;

        private float _currentPercent;
        private bool _isAnimate;

        public void SetValue(float value)
        {
            _isAnimate = false;
            _currentPercent = Mathf.Clamp01(value);
            if (bar != null) bar.fillAmount = _currentPercent;
            if (barBack != null) barBack.fillAmount = _currentPercent;
            if (healTrail != null) healTrail.fillAmount = _currentPercent;
        }

        public void AnimateValue(float value)
        {
            _currentPercent = Mathf.Clamp01(value);
            _isAnimate = true;
        }

        public void SetBarColor(Color color)
        {
            if (bar != null) bar.color = color;
        }

        private void Update()
        {
            if (!_isAnimate || bar == null) return;

            float fillBar = bar.fillAmount;
            float fillBack = barBack != null ? barBack.fillAmount : fillBar;

            if (fillBar < _currentPercent)
            {
                if (healTrail != null) healTrail.fillAmount = _currentPercent;
                if (barBack != null) barBack.fillAmount = _currentPercent;

                fillBar = Mathf.MoveTowards(fillBar, _currentPercent, GetSpeed(fillBar));
                bar.fillAmount = fillBar;

                if (Mathf.Approximately(fillBar, _currentPercent))
                    _isAnimate = false;
            }
            else if (fillBack > _currentPercent)
            {
                bar.fillAmount = _currentPercent;
                if (healTrail != null) healTrail.fillAmount = _currentPercent;

                fillBack = Mathf.MoveTowards(fillBack, _currentPercent, GetSpeed(fillBack));
                if (barBack != null) barBack.fillAmount = fillBack;

                if (Mathf.Approximately(fillBack, _currentPercent))
                    _isAnimate = false;
            }
            else
            {
                _isAnimate = false;
            }
        }

        private float GetSpeed(float pos)
        {
            return Time.deltaTime * Mathf.Clamp(Mathf.Abs(pos - _currentPercent) * speed, speedLimitMin, speedLimitMax);
        }
    }
}