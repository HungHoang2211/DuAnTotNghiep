using System.Collections;
using TMPro;
using UnityEngine;

namespace SimpleSurvival.UI.Hud
{
    public sealed class FollowNotifyPopup : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text label;

        [Header("Colors")]
        [SerializeField] private Color goodColor = Color.green;
        [SerializeField] private Color badColor = new Color(1f, 0.3f, 0.3f);
        [SerializeField] private Color neutralColor = Color.white;

        [Header("Slide")]
        [SerializeField] private Vector3 slideFrom = new Vector3(400f, -60f, 0f);
        [SerializeField] private Vector3 slideTo = new Vector3(220f, -60f, 0f);
        [SerializeField] private float slideDuration = 0.6f;

        [Header("Fade")]
        [SerializeField] private float fadeInDuration = 0.2f;
        [SerializeField] private float holdAfterSlide = 1f;
        [SerializeField] private float fadeOutDuration = 0.3f;

        public bool IsAnimated { get; private set; }

        private RectTransform _rect;
        private Coroutine _routine;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            canvasGroup.alpha = 0f;
        }

        public void Show(string text, SpeechHudType type)
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(ShowRoutine(text, type));
        }

        private IEnumerator ShowRoutine(string text, SpeechHudType type)
        {
            IsAnimated = true;
            label.text = text;
            label.color = GetColor(type);
            _rect.localPosition = slideFrom;
            canvasGroup.alpha = 0f;

            float elapsed = 0f;
            while (elapsed < slideDuration)
            {
                elapsed += Time.deltaTime;
                float slideT = Mathf.Clamp01(elapsed / slideDuration);
                _rect.localPosition = Vector3.Lerp(slideFrom, slideTo, slideT);

                float fadeT = Mathf.Clamp01(elapsed / fadeInDuration);
                canvasGroup.alpha = fadeT;

                yield return null;
            }

            _rect.localPosition = slideTo;
            canvasGroup.alpha = 1f;

            yield return new WaitForSeconds(holdAfterSlide);

            elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeOutDuration);
                canvasGroup.alpha = 1f - t;
                yield return null;
            }

            canvasGroup.alpha = 0f;
            label.text = string.Empty;
            _rect.localPosition = slideFrom;
            IsAnimated = false;
            _routine = null;
        }

        private Color GetColor(SpeechHudType type)
        {
            switch (type)
            {
                case SpeechHudType.Good: return goodColor;
                case SpeechHudType.Bad: return badColor;
                default: return neutralColor;
            }
        }
    }
}