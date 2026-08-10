using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleSurvival.World
{
    public class EndingComicReveal : MonoBehaviour
    {
        [SerializeField] private Image[] panels;
        [SerializeField] private float fadeDuration = 0.4f;

        [Header("Narration")]
        [Tooltip("AudioSource used to play each panel's line. playOnAwake should be OFF.")]
        [SerializeField] private AudioSource narrationSource;

        [Tooltip("Must be the same length/order as 'panels'. panelNarration[i] plays when panels[i] is revealed.")]
        [SerializeField] private AudioClip[] panelNarration;

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

            StopNarration();
        }

        public void RevealNext()
        {
            if (_fading || IsFullyRevealed) return;

            int index = _revealedCount;
            StartCoroutine(FadeInRoutine(panels[index]));
            PlayNarrationFor(index);
            _revealedCount++;
        }

        public void StopNarration()
        {
            if (narrationSource != null && narrationSource.isPlaying)
                narrationSource.Stop();
        }

        private void PlayNarrationFor(int index)
        {
            if (narrationSource == null || panelNarration == null) return;
            if (index < 0 || index >= panelNarration.Length) return;

            AudioClip clip = panelNarration[index];

            // Cut off whatever line was still playing, then start the new one.
            narrationSource.Stop();

            if (clip == null) return;

            narrationSource.clip = clip;
            narrationSource.Play();
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