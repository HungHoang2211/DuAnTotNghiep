using UnityEngine;

namespace Xyla.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerState))]
    public class TopDownMover : MonoBehaviour
    {
        private static readonly int AnimMoveSpeed = Animator.StringToHash("MoveSpeed");
        private static readonly int AnimIsSneaking = Animator.StringToHash("IsSneaking");

        [Header("Input")]
        [SerializeField] private PlayerInputReader _input;

        [Header("Movement")]
        [SerializeField] private float _walkSpeed = 3f;
        [SerializeField] private float _runSpeed = 6f;
        [SerializeField] private float _sneakSpeed = 1.5f;
        [SerializeField] private float _acceleration = 60f;

        [Header("Sneak")]
        [Tooltip("Bao nhiêu đơn vị sẽ co height xuống khi sneak (không phải height tuyệt đối).")]
        [SerializeField] private float _sneakHeightReduction = 0.6f;
        [Tooltip("Tốc độ lerp height collider.")]
        [SerializeField] private float _sneakLerpSpeed = 10f;

        [Header("Camera")]
        [SerializeField] private Transform _cameraTransform;

        [Header("Gravity")]
        [SerializeField] private float _gravity = -20f;

        [Header("Rotation")]
        [SerializeField] private float _rotationSpeed = 15f;
        [SerializeField] private float _modelYawOffset = 0f;

        [Header("Animation (optional)")]
        [SerializeField] private Animator _animator;
        [SerializeField] private float _speedDampTime = 0.1f;

        private CharacterController _cc;
        private Vector3 _horizontalVelocity;
        private float _verticalVelocity;

        // Lưu height gốc của CharacterController (lấy từ Inspector, không override)
        private float _originalHeight;
        private float _originalCenterY;

        public bool IsMoving { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsSneaking { get; private set; }

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _originalHeight = _cc.height;
            _originalCenterY = _cc.center.y;
        }

        private void Update()
        {
            UpdateSneakState();
            ApplySneakCollider();

            Vector3 moveDir = ReadMovementDirection();
            UpdateMovementFlags(moveDir);

            float targetSpeed = IsSneaking ? _sneakSpeed
                              : IsRunning ? _runSpeed
                              : _walkSpeed;

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

        // ── Sneak ────────────────────────────────────────────────────────
        private void UpdateSneakState()
        {
            bool wantSneak = _input.SneakHeld;

            // Nếu muốn đứng dậy nhưng bị vật cản → ép giữ sneak và sync lại toggle
            if (!wantSneak && IsSneaking && !CanStandUp())
            {
                _input.ForceSneak(true);
                return;
            }

            IsSneaking = wantSneak;
        }

        private void ApplySneakCollider()
        {
            float targetHeight = IsSneaking
                ? _originalHeight - _sneakHeightReduction
                : _originalHeight;

            float prevHeight = _cc.height;
            _cc.height = Mathf.Lerp(_cc.height, targetHeight, _sneakLerpSpeed * Time.deltaTime);

            float delta = _cc.height - prevHeight;
            _cc.center = new Vector3(0f, _cc.center.y + delta * 0.5f, 0f);
        }

        private bool CanStandUp()
        {
            float radius = _cc.radius * 0.9f;
            float checkDist = _sneakHeightReduction;
            Vector3 origin = transform.position
                             + Vector3.up * (_cc.height + 0.05f);
            return !Physics.SphereCast(origin, radius, Vector3.up, out _,
                                       checkDist, ~LayerMask.GetMask("Player"),
                                       QueryTriggerInteraction.Ignore);
        }

        // ── Movement ──────────────────────────────────────────────────────
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
                    Debug.LogWarning("[TopDownMover] Không tìm thấy Camera Transform.", this);
                    _warnedNoCamera = true;
                }
                dir = new Vector3(axis.x, 0f, axis.y);
            }
            else
            {
                Vector3 camForward = cam.forward; camForward.y = 0f; camForward.Normalize();
                Vector3 camRight = cam.right; camRight.y = 0f; camRight.Normalize();
                dir = camRight * axis.x + camForward * axis.y;
            }

            if (dir.sqrMagnitude > 1f) dir.Normalize();
            return dir;
        }

        private void UpdateMovementFlags(Vector3 dir)
        {
            IsMoving = dir.sqrMagnitude > 0.01f;
            IsRunning = IsMoving && !IsSneaking && _input.SprintHeld;
        }

        private void RotateTowardsMoveDirection(Vector3 moveDir)
        {
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
            _animator.SetBool(AnimIsSneaking, IsSneaking);
        }
    }
}