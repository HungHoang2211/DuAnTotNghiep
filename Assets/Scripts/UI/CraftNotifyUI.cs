using System.Collections;
using TMPro;
using UnityEngine;

namespace SimpleSurvival.UI
{
    public sealed class CraftNotifyUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text label;
        [SerializeField] private float fadeInDuration = 0.2f;
        [SerializeField] private float holdDuration = 1f;
        [SerializeField] private float fadeOutDuration = 0.3f;

        private Coroutine showRoutine;

        private void Awake()
        {
            canvasGroup.alpha = 0f;
        }

        private void OnDisable()
        {
            if (showRoutine != null)
            {
                StopCoroutine(showRoutine);
                showRoutine = null;
            }
            canvasGroup.alpha = 0f;
        }

        public void Show(string text)
        {
            if (showRoutine != null)
                StopCoroutine(showRoutine);

            showRoutine = StartCoroutine(ShowRoutine(text));
        }

        private IEnumerator ShowRoutine(string text)
        {
            label.text = text;

            yield return Fade(0f, 1f, fadeInDuration);
            yield return new WaitForSeconds(holdDuration);
            yield return Fade(1f, 0f, fadeOutDuration);

            showRoutine = null;
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            canvasGroup.alpha = to;
        }
    }
}