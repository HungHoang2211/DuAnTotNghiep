using UnityEngine;

namespace Xyla.Player
{
    public class MouseAimer : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private float _rotationSpeedDegPerSec = 720f;
        [SerializeField] private float _aimPlaneYOffset = 0f;

        [Tooltip("Tắt MouseAimer trên mobile (touch device). " +
                 "Trên mobile hướng nhìn do joystick/movement quyết định.")]
        [SerializeField] private bool _disableOnMobile = true;

        [Tooltip("Kéo MobileJoystick vào đây. Nếu joystick IsPressed thì MouseAimer nhường quyền xoay.")]
        [SerializeField] private MobileJoystick _mobileJoystick;

        private Camera _camera;
        private bool _isActive;
        private Rigidbody _rigidbody;

        private void Awake()
        {
            _camera = Camera.main;
            _isActive = !(_disableOnMobile && IsMobilePlatform());
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            if (!_isActive) return;
            // Nhường quyền xoay cho MovementAimer khi đang dùng joystick
            if (_mobileJoystick != null && _mobileJoystick.IsPressed) return;
            if (!HasValidMousePosition()) return;
            if (!TryGetAimPointOnGround(out Vector3 aimPoint)) return;

            Vector3 lookDirection = FlattenToHorizontal(aimPoint - transform.position);
            if (IsTooSmallToAim(lookDirection)) return;

            RotateToward(lookDirection);
        }

        private bool HasValidMousePosition()
        {
            Vector3 pos = _input.MouseScreenPosition;
            if (float.IsInfinity(pos.x) || float.IsInfinity(pos.y)) return false;
            if (float.IsNaN(pos.x) || float.IsNaN(pos.y)) return false;

            if (_camera == null) return false;
            var rect = _camera.pixelRect;
            return pos.x >= rect.xMin && pos.x <= rect.xMax
                && pos.y >= rect.yMin && pos.y <= rect.yMax;
        }

        private bool TryGetAimPointOnGround(out Vector3 aimPoint)
        {
            float planeY = transform.position.y + _aimPlaneYOffset;
            Plane aimPlane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));
            Ray ray = _camera.ScreenPointToRay(_input.MouseScreenPosition);

            if (aimPlane.Raycast(ray, out float distance))
            {
                aimPoint = ray.GetPoint(distance);
                return true;
            }

            aimPoint = default;
            return false;
        }

        private static Vector3 FlattenToHorizontal(Vector3 direction)
        {
            direction.y = 0f;
            return direction;
        }

        private static bool IsTooSmallToAim(Vector3 direction)
        {
            const float minSqrMagnitude = 0.0001f;
            return direction.sqrMagnitude < minSqrMagnitude;
        }

        private void RotateToward(Vector3 lookDirection)
        {
            // Chỉ xoay trục Y, tránh conflict với Rigidbody constraints
            float targetY = Quaternion.LookRotation(lookDirection).eulerAngles.y;
            float currentY = transform.eulerAngles.y;
            float newY = Mathf.MoveTowardsAngle(currentY, targetY, _rotationSpeedDegPerSec * Time.deltaTime);
            if (_rigidbody != null)
                _rigidbody.MoveRotation(Quaternion.Euler(0f, newY, 0f));
            else
                transform.rotation = Quaternion.Euler(0f, newY, 0f);
        }

        private static bool IsMobilePlatform()
        {
#if UNITY_IOS || UNITY_ANDROID
            return true;
#else
            return Input.touchSupported && !Input.mousePresent;
#endif
        }
    }
}