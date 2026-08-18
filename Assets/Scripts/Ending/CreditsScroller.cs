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
        [SerializeField] private bool loop = true;

        private RectTransform _rect;
        private float _timer;

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

            if (pos.y >= viewport.rect.height)
            {
                if (loop)
                    ResetPosition();
                else
                    enabled = false;
            }
        }
    }
}