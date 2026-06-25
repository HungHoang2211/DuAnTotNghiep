using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleSurvival.UI
{
    public sealed class StatLabel : MonoBehaviour
    {
        [SerializeField] private Animation statAnimation;
        [SerializeField] private TextMeshProUGUI amountText;
        [SerializeField] private Image iconStat;
        [SerializeField] private RectTransform cacheAmountTransform;
        [SerializeField] private Color decreaseColor = Color.red;
        [SerializeField] private Color increaseColor = Color.green;
        [SerializeField] private string animationClipName = "Stats_Show";

        private Vector3 _defaultPosition;
        private Vector3 _defaultScale;
        private Quaternion _defaultRotation;
        private Color _defaultTextColor;

        private void Awake()
        {
            InitCache();
        }

        private void OnEnable()
        {
            Reset();
        }

        private void InitCache()
        {
            if (cacheAmountTransform != null)
            {
                _defaultPosition = cacheAmountTransform.localPosition;
                _defaultScale = cacheAmountTransform.localScale;
                _defaultRotation = cacheAmountTransform.localRotation;
            }
            if (amountText != null)
                _defaultTextColor = amountText.color;
        }

        public void Reset()
        {
            StopAllCoroutines();
            if (cacheAmountTransform != null)
            {
                cacheAmountTransform.localPosition = _defaultPosition;
                cacheAmountTransform.localScale = _defaultScale;
                cacheAmountTransform.localRotation = _defaultRotation;
            }
            if (amountText != null) amountText.color = _defaultTextColor;
            if (iconStat != null) iconStat.color = _defaultTextColor;
        }

        public void SetAmount(int amount)
        {
            if (amountText != null)
                amountText.text = amount.ToString();
        }

        public void SetAmount(float amount)
        {
            if (amountText != null)
                amountText.text = $"{amount:F1}";
        }

        public void SetAmountAnimated(int amount, bool isIncrease)
        {
            Reset();
            SetAmount(amount);
            if (gameObject.activeInHierarchy)
                StartCoroutine(ShowAnimation(isIncrease ? increaseColor : decreaseColor));
        }

        public void SetAmountAnimated(float amount, bool isIncrease)
        {
            Reset();
            SetAmount(amount);
            if (gameObject.activeInHierarchy)
                StartCoroutine(ShowAnimation(isIncrease ? increaseColor : decreaseColor));
        }

        private IEnumerator ShowAnimation(Color color)
        {
            if (amountText != null) amountText.color = color;
            if (iconStat != null) iconStat.color = color;

            if (statAnimation != null)
            {
                statAnimation.Play(animationClipName);
                while (statAnimation.isPlaying)
                    yield return new WaitForEndOfFrame();
            }

            if (amountText != null) amountText.color = _defaultTextColor;
            if (iconStat != null) iconStat.color = _defaultTextColor;
        }
    }
}