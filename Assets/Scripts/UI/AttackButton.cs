using UnityEngine;
using UnityEngine.EventSystems;
using SimpleSurvival.Player;
using SimpleSurvival.Targets;
using SimpleSurvival.Actions;

namespace SimpleSurvival.UI
{
    public class AttackButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("References")]
        [SerializeField] private PlayerActionController actionController;
        [SerializeField] private PlayerTargetChecker targetChecker;
        [SerializeField] private Transform pressRoot;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Visual States")]
        [SerializeField, Range(0f, 1f)] private float availableAlpha = 1f;
        [SerializeField, Range(0f, 1f)] private float unavailableAlpha = 0.3f;
        [SerializeField] private float fadeSpeed = 8f;

        [Header("Behavior")]
        [SerializeField] private bool requireEnemyToAttack = true;

        private bool _isPressed;
        private bool _isAvailable;
        private float _currentAlpha;

        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            _currentAlpha = unavailableAlpha;
            ApplyAlpha();
        }

        private void OnEnable()
        {
            if (targetChecker != null)
                targetChecker.OnEnemyChanged += HandleEnemyChanged;

            SetAvailable(targetChecker != null && targetChecker.CurrentEnemy != null);
        }

        private void OnDisable()
        {
            if (targetChecker != null)
                targetChecker.OnEnemyChanged -= HandleEnemyChanged;
        }

        private void Update()
        {
            UpdateAlphaFade();

            if (!_isPressed) return;
            if (actionController.CurrentAction.Type == ActionType.Attack) return;
            if (requireEnemyToAttack && !_isAvailable) return;

            if (_isAvailable)
                actionController.RequestAttack(targetChecker.CurrentEnemy);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (canvasGroup != null && !canvasGroup.interactable) return;

            _isPressed = true;
            if (pressRoot != null) pressRoot.localScale = Vector3.one * 0.9f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isPressed = false;
            if (pressRoot != null) pressRoot.localScale = Vector3.one;
        }

        private void HandleEnemyChanged(ITargetable enemy)
        {
            SetAvailable(enemy != null);
        }

        private void SetAvailable(bool available)
        {
            _isAvailable = available;

            if (canvasGroup != null)
            {
                canvasGroup.interactable = available;
                canvasGroup.blocksRaycasts = available;
            }
        }

        private void UpdateAlphaFade()
        {
            if (canvasGroup == null) return;

            float target = _isAvailable ? availableAlpha : unavailableAlpha;
            if (Mathf.Approximately(_currentAlpha, target)) return;

            _currentAlpha = Mathf.MoveTowards(_currentAlpha, target, fadeSpeed * Time.deltaTime);
            ApplyAlpha();
        }

        private void ApplyAlpha()
        {
            if (canvasGroup != null)
                canvasGroup.alpha = _currentAlpha;
        }
    }
}