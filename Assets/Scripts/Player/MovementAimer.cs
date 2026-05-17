using UnityEngine;

namespace Xyla.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public class MovementAimer : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private float _rotationSpeedDegPerSec = 720f;

        [Tooltip("Camera dùng để tính hướng relative. Để trống = Camera.main.")]
        [SerializeField] private Camera _camera;

        private Rigidbody _rigidbody;

        private void Awake()
        {
            if (_camera == null) _camera = Camera.main;
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            Vector2 axis = _input.MovementAxis;
            if (axis.sqrMagnitude < 0.01f) return;

            // Tính hướng dựa trên yaw camera (bỏ qua pitch)
            float camYaw = _camera.transform.eulerAngles.y;
            Quaternion flatRotation = Quaternion.Euler(0f, camYaw, 0f);
            Vector3 forward = flatRotation * Vector3.forward;
            Vector3 right = flatRotation * Vector3.right;

            Vector3 moveDir = (forward * axis.y + right * axis.x).normalized;
            if (moveDir.sqrMagnitude < 0.001f) return;

            // Xoay chỉ trục Y qua Rigidbody — không đụng X/Z
            float targetY = Quaternion.LookRotation(moveDir).eulerAngles.y;
            float currentY = _rigidbody.rotation.eulerAngles.y;
            float newY = Mathf.MoveTowardsAngle(currentY, targetY, _rotationSpeedDegPerSec * Time.fixedDeltaTime);
            _rigidbody.MoveRotation(Quaternion.Euler(0f, newY, 0f));
        }
    }
}