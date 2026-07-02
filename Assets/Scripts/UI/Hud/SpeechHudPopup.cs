using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SimpleSurvival.Core;

namespace SimpleSurvival.UI.Hud
{
    public sealed class SpeechHudPopup : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text label;
        [SerializeField] private RectTransform rootLayout;

        [Header("Colors")]
        [SerializeField] private Color goodColor = Color.green;
        [SerializeField] private Color badColor = new Color(1f, 0.6f, 0.2f);
        [SerializeField] private Color neutralColor = Color.white;

        [Header("Timing")]
        [SerializeField] private float showTime = 2f;
        [SerializeField] private float fadeDuration = 0.3f;

        public Transform FollowTarget { get; private set; }
        public event Action<SpeechHudPopup> OnHidden;

        private RectTransform _canvasRect;
        private Camera _gameCamera;
        private Camera _uiCamera;
        private Vector3 _worldOffset;

        private float _showTimeRemaining;
        private bool _isShowing;
        private bool _updatePos;
        private Coroutine _fadeRoutine;

        public void Show(Transform target, Vector3 worldOffset, string text, SpeechHudType type,
                          RectTransform canvasRect, Camera gameCam, Camera uiCam)
        {
            FollowTarget = target;
            _worldOffset = worldOffset;
            _canvasRect = canvasRect;
            _gameCamera = gameCam;
            _uiCamera = uiCam;

            label.text = text;
            label.color = GetColor(type);

            if (rootLayout != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rootLayout);

            Vector3 lp = transform.localPosition;
            lp.z = 0f;
            transform.localPosition = lp;

            _showTimeRemaining = showTime;
            _updatePos = true;
            UpdatePosition();

            if (!_isShowing)
            {
                _isShowing = true;
                StopFade();
                _fadeRoutine = StartCoroutine(FadeRoutine(canvasGroup.alpha, 1f, null));
            }
        }

        private void Update()
        {
            if (!_isShowing) return;

            _showTimeRemaining -= Time.deltaTime;
            if (_showTimeRemaining <= 0f)
            {
                _isShowing = false;
                StopFade();
                _fadeRoutine = StartCoroutine(FadeRoutine(canvasGroup.alpha, 0f, HandleFadeOutComplete));
            }
        }

        private void LateUpdate()
        {
            if (_updatePos) UpdatePosition();
        }

        private void UpdatePosition()
        {
            if (FollowTarget == null || _canvasRect == null || _gameCamera == null) return;

            Vector3 worldPos = FollowTarget.position + _worldOffset;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(_gameCamera, worldPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, screenPoint, _uiCamera, out Vector2 localPoint);

            transform.localPosition = new Vector3(localPoint.x, localPoint.y, 0f);
        }

        private void HandleFadeOutComplete()
        {
            _updatePos = false;
            FollowTarget = null;
            OnHidden?.Invoke(this);
        }

        private IEnumerator FadeRoutine(float from, float to, Action onComplete)
        {
            canvasGroup.alpha = from;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                canvasGroup.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }
            canvasGroup.alpha = to;
            onComplete?.Invoke();
        }

        private void StopFade()
        {
            if (_fadeRoutine != null) { StopCoroutine(_fadeRoutine); _fadeRoutine = null; }
        }

        private Color GetColor(SpeechHudType type)
        {
            switch (type)
            {
                case SpeechHudType.Good: return goodColor;
                case SpeechHudType.Bad: return badColor;
                default: return neutralColor;
            }
        }

        private void OnReturnToPool()
        {
            _isShowing = false;
            _updatePos = false;
            StopFade();
            canvasGroup.alpha = 0f;
            FollowTarget = null;
        }
    }
}