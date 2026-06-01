using UnityEngine;

namespace Xyla.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class TopDownMover : MonoBehaviour
    {
        private static readonly int AnimMoveSpeed = Animator.StringToHash("MoveSpeed");

        [Header("Input")]
        [SerializeField] private PlayerInputReader _input;

        [Header("Movement")]
        [SerializeField] private float _walkSpeed = 3f;
        [SerializeField] private float _runSpeed = 6f;
        [SerializeField] private float _acceleration = 60f;

        [Header("Camera")]
        [Tooltip("Transform của camera (kéo Camera hoặc CameraRig vào). " +
                 "Dùng để input khớp với hướng nhìn trên màn hình. " +
                 "Để trống sẽ tự lấy Camera.main.")]
        [SerializeField] private Transform _cameraTransform;

        [Header("Gravity")]
        [SerializeField] private float _gravity = -20f;

        [Header("Rotation")]
        [Tooltip("Tốc độ xoay mặt player theo hướng di chuyển.")]
        [SerializeField] private float _rotationSpeed = 15f;

        [Tooltip("Bù góc hướng mặt nếu model không quay mặt về trục +Z. " +
                 "Đi lên mà mặt quay trái/phải thì chỉnh ô này (thử ±90, 180) " +
                 "cho tới khi mặt nhìn đúng hướng đi.")]
        [SerializeField] private float _modelYawOffset = 0f;

        [Header("Animation (optional)")]
        [SerializeField] private Animator _animator;
        [SerializeField] private float _speedDampTime = 0.1f;

        private CharacterController _cc;
        private Vector3 _horizontalVelocity;
        private float _verticalVelocity;

        public bool IsMoving { get; private set; }
        public bool IsRunning { get; private set; }

        private void Awake() => _cc = GetComponent<CharacterController>();

        private void Update()
        {
            Vector3 moveDir = ReadMovementDirection();
            UpdateMovementFlags(moveDir);

            float targetSpeed = IsRunning ? _runSpeed : _walkSpeed;
            Vector3 desiredHorizontal = moveDir * (IsMoving ? targetSpeed : 0f);

            _horizontalVelocity = Vector3.MoveTowards(
                _horizontalVelocity, desiredHorizontal, _acceleration * Time.deltaTime);

            if (_cc.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;
            else
                _verticalVelocity += _gravity * Time.deltaTime;

            _cc.Move((_horizontalVelocity + Vector3.up * _verticalVelocity) * Time.deltaTime);

            if (IsMoving) RotateTowardsMoveDirection(moveDir);

            UpdateAnimator();
        }

        private bool _warnedNoCamera;

        private Vector3 ReadMovementDirection()
        {
            Vector2 axis = _input.MovementAxis;

            Transform cam = _cameraTransform != null
                ? _cameraTransform
                : (Camera.main != null ? Camera.main.transform : null);

            Vector3 dir;
            if (cam == null)
            {
                if (!_warnedNoCamera)
                {
                    Debug.LogWarning(
                        "[TopDownMover] Chưa gán Camera Transform và không tìm thấy " +
                        "Camera.main → input sẽ theo world space và có thể bị lệch. " +
                        "Hãy kéo Camera vào ô Camera Transform.", this);
                    _warnedNoCamera = true;
                }
                dir = new Vector3(axis.x, 0f, axis.y);
            }
            else
            {
                // Lấy forward/right của camera rồi ép phẳng xuống mặt đất (bỏ trục Y).
                // Nhờ vậy joystick khớp đúng hướng nhìn: lên = đi xa camera (lên màn hình),
                // phải = sang phải màn hình — không phụ thuộc góc nghiêng/yaw của camera.
                Vector3 camForward = cam.forward;
                Vector3 camRight = cam.right;
                camForward.y = 0f;
                camRight.y = 0f;
                camForward.Normalize();
                camRight.Normalize();

                dir = camRight * axis.x + camForward * axis.y;
            }

            if (dir.sqrMagnitude > 1f) dir.Normalize();
            return dir;
        }

        private void UpdateMovementFlags(Vector3 dir)
        {
            IsMoving = dir.sqrMagnitude > 0.01f;
            IsRunning = IsMoving && _input.SprintHeld;
        }

        private void RotateTowardsMoveDirection(Vector3 moveDir)
        {
            // LookRotation cho transform.+Z hướng theo moveDir.
            // Nhân thêm _modelYawOffset để bù trường hợp mesh không quay mặt về +Z.
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up)
                                   * Quaternion.Euler(0f, _modelYawOffset, 0f);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, targetRot, _rotationSpeed * Time.deltaTime);
        }

        private void UpdateAnimator()
        {
            if (_animator == null) return;
            float target = !IsMoving ? 0f : IsRunning ? 1f : 0.5f;
            _animator.SetFloat(AnimMoveSpeed, target, _speedDampTime, Time.deltaTime);
        }
    }
}