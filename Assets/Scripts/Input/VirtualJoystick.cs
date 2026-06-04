using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SimpleSurvival.Input
{
    /// <summary>
    /// Floating joystick kiểu LDOE: nơi tay người chơi chạm đầu tiên là tâm joystick,
    /// kéo từ đó để di chuyển. Output là Vector2 normalized direction + magnitude (0..1).
    /// 
    /// Gắn lên GameObject "Joystick_Background" trong UI Canvas.
    /// </summary>
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

        // Public output cho PlayerInput đọc
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

            // Nếu Canvas dùng Screen Space - Camera, cần camera để convert screen → local point
            if (_canvas.renderMode == RenderMode.ScreenSpaceCamera)
                _uiCamera = _canvas.worldCamera;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            IsPressed = true;

            if (isFloating)
            {
                // Floating mode: di chuyển background đến vị trí ngón tay chạm
                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    background.parent as RectTransform,
                    eventData.position,
                    _uiCamera,
                    out localPoint
                );
                background.anchoredPosition = localPoint;
            }

            // Drag ngay từ pointer down (không đợi drag event)
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Tính vector từ tâm background đến vị trí ngón tay (trong local space của background)
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                background,
                eventData.position,
                _uiCamera,
                out localPoint
            );

            // Normalize theo size của background, clamp trong vòng tròn
            float radius = background.sizeDelta.x * 0.5f * handleRange;
            Vector2 offset = Vector2.ClampMagnitude(localPoint, radius);

            handle.anchoredPosition = offset;

            // Output: direction normalized + magnitude 0..1
            Direction = offset.normalized;
            Magnitude = offset.magnitude / radius;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            IsPressed = false;

            // Reset handle về tâm
            handle.anchoredPosition = Vector2.zero;
            Direction = Vector2.zero;
            Magnitude = 0f;

            // Reset background về vị trí default (chỉ với floating mode)
            if (isFloating)
                background.anchoredPosition = _defaultBackgroundPos;
        }
    }
}