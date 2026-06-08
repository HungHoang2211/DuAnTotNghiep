using UnityEngine;
using UnityEngine.EventSystems;

namespace Xyla.Player
{
    /// SETUP:
    ///   Gắn script này lên JoystickArea (cùng object nhận PointerDown/Up).
    ///   Kéo CanvasGroup của Joystick background vào _joystickGroup.
    ///   Nếu để trống, script tự tìm CanvasGroup trên GameObject hiện tại.
    public class JoystickVisibility : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Target")]
        [Tooltip("CanvasGroup của Joystick background. Để trống → tự tìm trên GameObject này.")]
        [SerializeField] private CanvasGroup _joystickGroup;

        [Header("Alpha")]
        [SerializeField][Range(0f, 1f)] private float _activeAlpha = 0.9f;
        [SerializeField][Range(0f, 1f)] private float _idleAlpha = 0f;

        [Tooltip("Tốc độ fade (alpha/giây). 0 = ngay lập tức.")]
        [SerializeField] private float _fadeSpeed = 8f;

        private float _targetAlpha;

        private void Awake()
        {
            if (_joystickGroup == null)
                _joystickGroup = GetComponent<CanvasGroup>();

            if (_joystickGroup == null)
            {
                Debug.LogWarning($"[JoystickVisibility] Không tìm thấy CanvasGroup trên {name}. " +
                                 "Hãy gán thủ công vào _joystickGroup.");
            }

            _targetAlpha = _idleAlpha;
            ApplyAlpha(_idleAlpha);
        }

        private void Update()
        {
            if (_joystickGroup == null) return;

            if (_fadeSpeed <= 0f)
            {
                ApplyAlpha(_targetAlpha);
                return;
            }

            _joystickGroup.alpha = Mathf.MoveTowards(
                _joystickGroup.alpha,
                _targetAlpha,
                _fadeSpeed * Time.unscaledDeltaTime); 
        }


        public void OnPointerDown(PointerEventData eventData)
        {
            _targetAlpha = _activeAlpha;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _targetAlpha = _idleAlpha;
        }


        public void ShowImmediate() => ApplyAlpha(_activeAlpha);

        public void HideImmediate() => ApplyAlpha(_idleAlpha);
        private void ApplyAlpha(float alpha)
        {
            if (_joystickGroup == null) return;
            _joystickGroup.alpha = alpha;
            _targetAlpha = alpha;
        }
    }
}