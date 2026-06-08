using UnityEngine;
using SimpleSurvival.Input;

namespace SimpleSurvival.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInputReader))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Speeds")]
        [SerializeField] private float walkSpeed = 3f;
        [SerializeField] private float runSpeed = 6f;
        [SerializeField] private float sneakSpeed = 1.5f;

        [Tooltip("Magnitude joystick từ ngưỡng này trở lên = chạy. Dưới = đi bộ.")]
        [SerializeField, Range(0.3f, 0.95f)] private float runThreshold = 0.6f;

        [Header("Acceleration")]
        [Tooltip("Đơn vị/giây² — cao = đổi tốc độ nhanh, thấp = trễ.")]
        [SerializeField] private float acceleration = 60f;

        [Header("Rotation")]
        [SerializeField, Range(1f, 30f)] private float rotationSmoothness = 12f;

        [Header("Sneak Collider")]
        [Tooltip("Capsule co bao nhiêu đơn vị khi sneak.")]
        [SerializeField] private float sneakHeightReduction = 0.6f;

        [Tooltip("Tốc độ lerp collider khi sneak/đứng.")]
        [SerializeField] private float sneakLerpSpeed = 10f;

        [Tooltip("Layer mà sphere cast SẼ va vào khi check đứng dậy (bỏ tick Player).")]
        [SerializeField] private LayerMask standUpCheckMask = ~0;

        [Header("Gravity")]
        [SerializeField] private float gravity = -20f;

        public bool IsMoving { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsSneaking { get; private set; }

        public float NormalizedSpeed { get; private set; }

        private CharacterController _controller;
        private PlayerInputReader _input;

        private Vector3 _horizontalVelocity;
        private float _verticalVelocity;

        private float _originalHeight;
        private float _originalCenterY;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<PlayerInputReader>();
            _originalHeight = _controller.height;
            _originalCenterY = _controller.center.y;
        }

        private void Update()
        {
            UpdateSneakState();
            ApplySneakCollider();
            UpdateMovement();
            UpdateRotation();
        }

        private void UpdateSneakState()
        {
            bool wantSneak = _input.SneakHeld;

            if (!wantSneak && IsSneaking && !CanStandUp())
            {
                _input.ForceSneak(true);
                IsSneaking = true;
                return;
            }

            IsSneaking = wantSneak;
        }

        private void ApplySneakCollider()
        {
            float targetHeight = IsSneaking
                ? _originalHeight - sneakHeightReduction
                : _originalHeight;

            float prevHeight = _controller.height;
            _controller.height = Mathf.Lerp(_controller.height, targetHeight, sneakLerpSpeed * Time.deltaTime);

            float delta = _controller.height - prevHeight;
            _controller.center = new Vector3(0f, _controller.center.y + delta * 0.5f, 0f);
        }

        private bool CanStandUp()
        {
            float radius = _controller.radius * 0.9f;
            Vector3 origin = transform.position + Vector3.up * (_controller.height + 0.05f);
            return !Physics.SphereCast(
                origin, radius, Vector3.up, out _,
                sneakHeightReduction, standUpCheckMask, QueryTriggerInteraction.Ignore
            );
        }

        private void UpdateMovement()
        {
            Vector3 moveDir = _input.WorldDirection;
            float inputMagnitude = _input.Magnitude;

            IsMoving = inputMagnitude > 0.1f;
            IsRunning = IsMoving && !IsSneaking && inputMagnitude >= runThreshold;

            float targetSpeed = IsSneaking ? sneakSpeed
                              : IsRunning ? runSpeed
                              : walkSpeed;

            Vector3 desiredVelocity = IsMoving ? moveDir * targetSpeed : Vector3.zero;

            _horizontalVelocity = Vector3.MoveTowards(
                _horizontalVelocity, desiredVelocity, acceleration * Time.deltaTime
            );

            if (_controller.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;
            else
                _verticalVelocity += gravity * Time.deltaTime;

            Vector3 totalVelocity = _horizontalVelocity + Vector3.up * _verticalVelocity;
            _controller.Move(totalVelocity * Time.deltaTime);

            float maxSpeed = IsRunning ? runSpeed : (IsSneaking ? sneakSpeed : walkSpeed);
            NormalizedSpeed = maxSpeed > 0.01f ? _horizontalVelocity.magnitude / maxSpeed : 0f;
        }


        private void UpdateRotation()
        {
            if (!IsMoving) return;

            Quaternion targetRot = Quaternion.LookRotation(_input.WorldDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, targetRot, rotationSmoothness * Time.deltaTime
            );
        }
    }
}