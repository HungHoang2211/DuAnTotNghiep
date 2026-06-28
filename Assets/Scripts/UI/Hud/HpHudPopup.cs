using System.Collections;
using TMPro;
using UnityEngine;
using SimpleSurvival.Core;

namespace SimpleSurvival.UI.Hud
{
    public sealed class HpHudPopup : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text label;

        [Header("Colors")]
        [SerializeField] private Color damageColor = new Color(1f, 0.2f, 0.2f);
        [SerializeField] private Color damageEnemyColor = Color.white;
        [SerializeField] private Color healColor = Color.green;

        [Header("Animation")]
        [SerializeField] private float lifetime = 1.3f;
        [SerializeField] private float floatUpDistance = 30f;
        [SerializeField] private float alphaDelay = 0.3f;
        [SerializeField] private float alphaFadeDuration = 1.0f;
        [SerializeField] private Vector3 scaleStart = new Vector3(0.8f, 0.8f, 0.8f);
        [SerializeField] private Vector3 scalePeak = new Vector3(1.2f, 1.2f, 1.2f);
        [SerializeField] private Vector3 scaleEnd = Vector3.one;
        [SerializeField] private float scalePunchDuration = 0.5f;

        [Header("Spread")]
        [SerializeField] private float randomXMin = -50f;
        [SerializeField] private float randomXMax = 50f;
        [SerializeField] private float randomYMin = 150f;
        [SerializeField] private float randomYMax = 180f;

        private Transform _followTarget;
        private RectTransform _canvasRect;
        private Camera _gameCamera;
        private Camera _uiCamera;
        private Vector3 _worldOffset;

        private RectTransform _labelRect;
        private Coroutine _animRoutine;
        private Coroutine _scaleRoutine;
        private Vector3 _startLocalPos;
        private bool _updatePos;

        private void Awake()
        {
            if (label != null)
                _labelRect = label.rectTransform;
        }

        public void Show(Transform followTarget, Vector3 worldOffset, RectTransform canvasRect,
                         Camera gameCam, Camera uiCam, string text, HpHudType type)
        {
            _followTarget = followTarget;
            _worldOffset = worldOffset;
            _canvasRect = canvasRect;
            _gameCamera = gameCam;
            _uiCamera = uiCam;

            Vector3 rootLocalPos = transform.localPosition;
            rootLocalPos.z = 0f;
            transform.localPosition = rootLocalPos;

            label.text = text;
            label.color = GetColor(type);

            float xOff = Random.Range(randomXMin, randomXMax);
            float yOff = Random.Range(randomYMin, randomYMax);
            _startLocalPos = new Vector3(xOff, yOff, 0f);
            _labelRect.localPosition = _startLocalPos;
            _labelRect.localScale = scaleStart;

            _updatePos = true;
            UpdatePosition();

            StopRoutines();
            _animRoutine = StartCoroutine(AnimateRoutine());
            _scaleRoutine = StartCoroutine(ScalePunchRoutine());
        }

        private void LateUpdate()
        {
            if (_updatePos) UpdatePosition();
        }

        private void UpdatePosition()
        {
            if (_followTarget == null || _canvasRect == null || _gameCamera == null) return;

            Vector3 worldPos = _followTarget.position + _worldOffset;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(_gameCamera, worldPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, screenPoint, _uiCamera, out Vector2 localPoint);

            transform.localPosition = new Vector3(localPoint.x, localPoint.y, 0f);
        }

        private IEnumerator AnimateRoutine()
        {
            canvasGroup.alpha = 1f;

            float elapsed = 0f;
            Vector3 fromPos = _startLocalPos;
            Vector3 toPos = fromPos + new Vector3(0f, floatUpDistance, 0f);

            while (elapsed < lifetime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / lifetime);
                _labelRect.localPosition = Vector3.Lerp(fromPos, toPos, t);

                if (elapsed > alphaDelay)
                {
                    float fadeT = Mathf.Clamp01((elapsed - alphaDelay) / alphaFadeDuration);
                    canvasGroup.alpha = 1f - fadeT;
                }

                yield return null;
            }

            canvasGroup.alpha = 0f;
            ReturnToPool();
        }

        private IEnumerator ScalePunchRoutine()
        {
            float halfDur = scalePunchDuration * 0.5f;
            float elapsed = 0f;

            while (elapsed < halfDur)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDur;
                _labelRect.localScale = Vector3.Lerp(scaleStart, scalePeak, t);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < halfDur)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDur;
                _labelRect.localScale = Vector3.Lerp(scalePeak, scaleEnd, t);
                yield return null;
            }

            _labelRect.localScale = scaleEnd;
        }

        private Color GetColor(HpHudType type)
        {
            switch (type)
            {
                case HpHudType.Damage: return damageColor;
                case HpHudType.DamageEnemy: return damageEnemyColor;
                case HpHudType.Heal: return healColor;
                default: return Color.white;
            }
        }

        private void StopRoutines()
        {
            if (_animRoutine != null) { StopCoroutine(_animRoutine); _animRoutine = null; }
            if (_scaleRoutine != null) { StopCoroutine(_scaleRoutine); _scaleRoutine = null; }
        }

        private void ReturnToPool()
        {
            _updatePos = false;
            StopRoutines();
            ObjectPool.Instance.Return(gameObject);
        }

        private void OnReturnToPool()
        {
            _updatePos = false;
            canvasGroup.alpha = 0f;
            _followTarget = null;
            StopRoutines();
        }
    }
}