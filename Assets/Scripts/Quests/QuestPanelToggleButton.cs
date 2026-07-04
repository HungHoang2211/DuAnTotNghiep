using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SimpleSurvival.Quests
{
    public sealed class QuestPanelToggleButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Target")]
        [SerializeField] private GameObject panelContent;
        [SerializeField] private GameObject icon;
        [SerializeField] private GameObject arrowPanel;

        [Header("Position")]
        [SerializeField] private Vector2 openAnchoredPosition;

        [Header("Press Feedback")]
        [SerializeField] private Animation pressAnimation;
        [SerializeField] private string downClipName = "OnDown";
        [SerializeField] private string upClipName = "OnUp";

        private RectTransform _rectTransform;
        private Vector2 _closedAnchoredPosition;
        private bool _isOpen;
        private Button _button;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _closedAnchoredPosition = _rectTransform.anchoredPosition;

            _button = GetComponent<Button>();
            if (_button != null)
                _button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(HandleClick);
        }

        private void Start()
        {
            _isOpen = false;
            ApplyState();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            PlayClip(downClipName);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            PlayClip(upClipName);
        }

        private void HandleClick()
        {
            _isOpen = !_isOpen;
            ApplyState();
        }

        private void ApplyState()
        {
            if (panelContent != null) panelContent.SetActive(_isOpen);
            if (icon != null) icon.SetActive(!_isOpen);
            if (arrowPanel != null) arrowPanel.SetActive(_isOpen);

            _rectTransform.anchoredPosition = _isOpen ? openAnchoredPosition : _closedAnchoredPosition;
        }

        private void PlayClip(string clipName)
        {
            if (pressAnimation == null || string.IsNullOrEmpty(clipName)) return;
            pressAnimation.Play(clipName);
        }
    }
}