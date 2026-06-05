using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SimpleSurvival.Input
{
    [RequireComponent(typeof(RectTransform))]
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("References")]
        [Tooltip("Image background của joystick (cùng GameObject này).")]
        [SerializeField] private RectTransform background;

        [Tooltip("Handle (núm) di chuyển bên trong background.")]
        [SerializeField] private RectTransform handle;

        [Header("Settings")]
        [Tooltip("Joystick có theo ngón tay không (LDOE style) hay đứng yên một chỗ.")]
        [SerializeField] private bool isFloating = true;

        [Tooltip("Bán kính tối đa handle có thể kéo ra khỏi tâm (% của background size).")]
        [SerializeField, Range(0.3f, 1f)] private float handleRange = 0.5f;
        public Vector2 Direction { get; private set; } = Vector2.zero;
        public float Magnitude { get; private set; } = 0f;
        public bool IsPressed { get; private set; } = false;

        private Vector2 _defaultBackgroundPos;
        private Canvas _canvas;
        private UnityEngine.Camera _uiCamera;

        private void Awake()
        {
            if (background == null) background = GetComponent<RectTransform>();

            _defaultBackgroundPos = background.anchoredPosition;
            _canvas = GetComponentInParent<Canvas>();

            if (_canvas.renderMode == RenderMode.ScreenSpaceCamera)
                _uiCamera = _canvas.worldCamera;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            IsPressed = true;

            if (isFloating)
            {
                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    background.parent as RectTransform,
                    eventData.position,
                    _uiCamera,
                    out localPoint
                );
                background.anchoredPosition = localPoint;
            }
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                background,
                eventData.position,
                _uiCamera,
                out localPoint
            );
            float radius = background.sizeDelta.x * 0.5f * handleRange;
            Vector2 offset = Vector2.ClampMagnitude(localPoint, radius);

            handle.anchoredPosition = offset;
            Direction = offset.normalized;
            Magnitude = offset.magnitude / radius;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            IsPressed = false;
            handle.anchoredPosition = Vector2.zero;
            Direction = Vector2.zero;
            Magnitude = 0f;
            if (isFloating)
                background.anchoredPosition = _defaultBackgroundPos;
        }
    }
}