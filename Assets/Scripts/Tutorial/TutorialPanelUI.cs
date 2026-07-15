using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleSurvival.UI.Tutorial
{
    public class TutorialPanelUI : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Image pageImage;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private List<TutorialPageData> pages;
        
        public event System.Action OnPanelClosed;
        private int currentIndex;

        private void Awake()
        {
            pages.Sort((a, b) => a.Order.CompareTo(b.Order));

            nextButton.onClick.AddListener(Next);
            previousButton.onClick.AddListener(Previous);
            closeButton.onClick.AddListener(ClosePanel);
        }

        public void OpenPanel()
        {
            currentIndex = 0;
            panelRoot.SetActive(true);
            UpdatePage();
            Time.timeScale = 0f;
        }

        public void ClosePanel()
        {
            panelRoot.SetActive(false);
            Time.timeScale = 1f;
            OnPanelClosed?.Invoke();
        }
        private void Next()
        {
            if (currentIndex >= pages.Count - 1) return;
            currentIndex++;
            UpdatePage();
        }

        private void Previous()
        {
            if (currentIndex <= 0) return;
            currentIndex--;
            UpdatePage();
        }

        private void UpdatePage()
        {
            pageImage.sprite = pages[currentIndex].PageImage;
            previousButton.interactable = currentIndex > 0;
            nextButton.interactable = currentIndex < pages.Count - 1;
        }
    }
}