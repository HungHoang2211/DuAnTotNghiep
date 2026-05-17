using UnityEngine;
using UnityEngine.EventSystems;

namespace Xyla.Player
{
    // SETUP:
    //   1. Gắn script này lên JoystickArea (UI GameObject, KHÔNG phải Player 3D).
    //   2. Kéo "Joystick" (background tròn) vào _joystickRoot.
    //  3. Kéo "Handle" vào _handle.
    //   4. Kéo MobileJoystick component này vào PlayerInputReader._mobileJoystick
   
    // Muốn ẩn joystick lúc idle → gắn thêm JoystickVisibility lên JoystickArea,
    // thêm CanvasGroup lên "Joystick", kéo vào _joystickGroup của JoystickVisibility.

    public class MobileJoystick : MonoBehaviour,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("References")]
        [Tooltip("RectTransform của Joystick background (hình tròn nền).")]
        [SerializeField] private RectTransform _joystickRoot;

        [Tooltip("RectTransform của Handle (nút nhỏ bên trong).")]
        [SerializeField] private RectTransform _handle;

        [Header("Behaviour")]
        [Tooltip("Bán kính tối đa handle có thể kéo (đơn vị UI pixel).")]
        [SerializeField] private float _radius = 60f;

        [Tooltip("True = joystick dịch chuyển tới điểm chạm (dynamic).\n" +
                 "False = joystick cố định tại vị trí đặt trong Editor.")]
        [SerializeField] private bool _followTouch = true;

        // Output
        public Vector2 Axis { get; private set; }
        public bool IsPressed { get; private set; }

        private Canvas _canvas;
        private Camera _uiCamera;
        private Vector2 _joystickStartAnchoredPos;

        private void Awake()
        {
            _canvas = GetComponentInParent<Canvas>();

            _uiCamera = (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? _canvas.worldCamera
                : null;

            if (_joystickRoot != null)
                _joystickStartAnchoredPos = _joystickRoot.anchoredPosition;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            IsPressed = true;

            if (_followTouch)
                MoveJoystickToTouch(eventData.position);

            UpdateHandle(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            UpdateHandle(eventData.position);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            IsPressed = false;
            Axis = Vector2.zero;

            if (_handle != null)
                _handle.anchoredPosition = Vector2.zero;

            if (_followTouch && _joystickRoot != null)
                _joystickRoot.anchoredPosition = _joystickStartAnchoredPos;
        }

        private void MoveJoystickToTouch(Vector2 screenPos)
        {
            if (_joystickRoot == null) return;
            var parent = _joystickRoot.parent as RectTransform;
            if (parent == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent, screenPos, _uiCamera, out Vector2 localPoint);

            _joystickRoot.anchoredPosition = localPoint;
        }

        private void UpdateHandle(Vector2 screenPos)
        {
            if (_handle == null || _joystickRoot == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _joystickRoot, screenPos, _uiCamera, out Vector2 localPoint);

            Vector2 clamped = Vector2.ClampMagnitude(localPoint, _radius);
            _handle.anchoredPosition = clamped;

            Axis = (_radius > 0f) ? clamped / _radius : Vector2.zero;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_joystickRoot == null) return;
            var c = GetComponentInParent<Canvas>();
            float scale = c != null ? c.scaleFactor : 1f;
            UnityEditor.Handles.color = new Color(0f, 1f, 0.5f, 0.5f);
            UnityEditor.Handles.DrawWireDisc(_joystickRoot.position, Vector3.back, _radius * scale);
        }
#endif
    }
}