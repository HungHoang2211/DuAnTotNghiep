using System.Collections;
using UnityEngine;

namespace SimpleSurvival.World
{
    public class FadeScreen : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private GameObject loadingIndicator;

        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            ShowLoading(true);
        }

        public void SetBlack()
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            ShowLoading(true);
        }

        public IEnumerator FadeOut(float duration)
        {
            canvasGroup.blocksRaycasts = true;
            ShowLoading(true);
            yield return Fade(canvasGroup.alpha, 1f, duration);
        }

        public IEnumerator FadeIn(float duration)
        {
            yield return Fade(canvasGroup.alpha, 0f, duration);
            ShowLoading(false);
            canvasGroup.blocksRaycasts = false;
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            if (duration <= 0f)
            {
                canvasGroup.alpha = to;
                yield break;
            }

            float time = 0f;
            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to, time / duration);
                yield return null;
            }

            canvasGroup.alpha = to;
        }

        private void ShowLoading(bool visible)
        {
            if (loadingIndicator != null)
                loadingIndicator.SetActive(visible);
        }
    }
}