using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleSurvival.World
{
    public class EndingComicReveal : MonoBehaviour
    {
        [SerializeField] private Image[] panels;
        [SerializeField] private float fadeDuration = 0.4f;

        private int _revealedCount;
        private bool _fading;

        public bool IsFullyRevealed => _revealedCount >= panels.Length;

        public void ResetReveal()
        {
            _revealedCount = 0;
            foreach (Image panel in panels)
            {
                Color c = panel.color;
                c.a = 0f;
                panel.color = c;
            }
        }

        public void RevealNext()
        {
            if (_fading || IsFullyRevealed) return;
            StartCoroutine(FadeInRoutine(panels[_revealedCount]));
            _revealedCount++;
        }

        private IEnumerator FadeInRoutine(Image panel)
        {
            _fading = true;
            float t = 0f;
            Color c = panel.color;

            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                c.a = Mathf.Clamp01(t / fadeDuration);
                panel.color = c;
                yield return null;
            }

            c.a = 1f;
            panel.color = c;
            _fading = false;
        }
    }
}