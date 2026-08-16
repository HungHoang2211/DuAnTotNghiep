using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleSurvival.SaveLoad;

namespace SimpleSurvival.World
{
    public class IntroComicUI : MonoBehaviour
    {
        public static IntroComicUI Instance { get; private set; }

        [SerializeField] private GameObject introVisualRoot;
        [SerializeField] private List<GameObject> pages = new List<GameObject>();
        [SerializeField] private List<EndingComicReveal> pageReveals = new List<EndingComicReveal>();

        private int _currentPageIndex;
        private bool _completed;
        private bool _isPlaying;
        private bool _closeRequested;

        public bool IsCompleted => _completed;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            introVisualRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Restore(IntroSaveData data)
        {
            _completed = data != null && data.completed;
            _currentPageIndex = data != null ? Mathf.Clamp(data.currentPage, 0, pages.Count - 1) : 0;
        }

        public IntroSaveData Capture()
        {
            return new IntroSaveData { completed = _completed, currentPage = _currentPageIndex };
        }

        public IEnumerator PlayRoutine()
        {
            if (_completed) yield break;

            _closeRequested = false;
            introVisualRoot.SetActive(true);
            OpenPage(_currentPageIndex);

            _isPlaying = true;
            while (!_closeRequested)
                yield return null;

            _completed = true;
            introVisualRoot.SetActive(false);
            _isPlaying = false;
        }

        private void OpenPage(int index)
        {
            _currentPageIndex = index;

            for (int i = 0; i < pages.Count; i++)
                pages[i].SetActive(i == index);

            pageReveals[index].ResetReveal();
            pageReveals[index].RevealNext();
        }

        public void OnTapAdvance()
        {
            if (!_isPlaying) return;

            EndingComicReveal currentReveal = pageReveals[_currentPageIndex];

            if (!currentReveal.IsFullyRevealed)
            {
                currentReveal.RevealNext();
                return;
            }

            if (_currentPageIndex < pages.Count - 1)
                OpenPage(_currentPageIndex + 1);
            else
                _closeRequested = true;
        }
    }
}