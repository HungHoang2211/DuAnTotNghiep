using System;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleSurvival.UI
{
    public sealed class UnlockProgressBar : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject root;
        [SerializeField] private Image progressBarImage;

        private Camera _gameCamera;
        private Camera _uiCamera;
        private RectTransform _canvasRect;
        private RectTransform _rect;
        private Transform _target;
        private Vector3 _worldOffset;
        private float _duration;
        private float _elapsed;
        private bool _running;

        public event Action OnComplete;

        private void Awake()
        {
            _rect = (RectTransform)transform;
        }

        public void Show(
            Transform target,
            Vector3 worldOffset,
            float duration,
            Camera gameCamera,
            Camera uiCamera,
            RectTransform canvasRect)
        {
            _target = target;
            _worldOffset = worldOffset;
            _duration = Mathf.Max(0.01f, duration);
            _elapsed = 0f;
            _gameCamera = gameCamera;
            _uiCamera = uiCamera;
            _canvasRect = canvasRect;
            _running = true;

            if (root != null) root.SetActive(true);
            if (progressBarImage != null) progressBarImage.fillAmount = 0f;

            UpdatePosition();
        }

        public void Stop()
        {
            _running = false;
            if (root != null) root.SetActive(false);
        }

        private void Update()
        {
            if (!_running) return;

            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);

            if (progressBarImage != null)
                progressBarImage.fillAmount = t;

            if (t >= 1f)
            {
                _running = false;
                Action callback = OnComplete;
                OnComplete = null;
                callback?.Invoke();
            }
        }

        private void LateUpdate()
        {
            if (!_running || _target == null) return;
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            if (_target == null || _gameCamera == null || _canvasRect == null) return;

            Vector3 worldPos = _target.position + _worldOffset;
            Vector3 screenPos = _gameCamera.WorldToScreenPoint(worldPos);

            if (screenPos.z < 0f)
            {
                if (root != null && root.activeSelf) root.SetActive(false);
                return;
            }
            if (root != null && !root.activeSelf) root.SetActive(true);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, screenPos, _uiCamera, out Vector2 localPos);
            _rect.anchoredPosition = localPos;
        }

        private void OnReturnToPool()
        {
            Stop();
            _target = null;
            OnComplete = null;
        }
    }
}