using System;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleSurvival.World
{
    [RequireComponent(typeof(RectTransform))]
    public class CreditsScroller : MonoBehaviour
    {
        [SerializeField] private RectTransform viewport;
        [SerializeField] private float scrollSpeed = 40f;
        [SerializeField] private float startDelay = 0.8f;
        [SerializeField] private bool loop = false;

        [Tooltip("anchoredPosition.y at which scrolling stops when loop is off. " +
                  "0 = stop as soon as the last line has fully entered the viewport. " +
                  "Increase to keep scrolling a bit further (last line ends higher up).")]
        [SerializeField] private float stopAtY = 0f;

        private RectTransform _rect;
        private float _timer;

        public event Action OnFinished;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            _timer = 0f;
            ResetPosition();
        }

        private void ResetPosition()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_rect);

            Vector2 pos = _rect.anchoredPosition;
            pos.y = -_rect.rect.height;
            _rect.anchoredPosition = pos;
        }

        private void Update()
        {
            _timer += Time.unscaledDeltaTime;
            if (_timer < startDelay) return;

            Vector2 pos = _rect.anchoredPosition;
            pos.y += scrollSpeed * Time.unscaledDeltaTime;
            _rect.anchoredPosition = pos;

            float target = loop ? viewport.rect.height : stopAtY;

            if (pos.y >= target)
            {
                if (loop)
                {
                    OnFinished?.Invoke();
                    ResetPosition();
                }
                else
                {
                    pos.y = target;
                    _rect.anchoredPosition = pos;
                    enabled = false;
                    OnFinished?.Invoke();
                }
            }
        }
    }
}