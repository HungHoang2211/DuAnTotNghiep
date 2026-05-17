using UnityEngine;

namespace Xyla.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public class TopDownMover : MonoBehaviour
    {
        private static readonly int SpeedParam = Animator.StringToHash("Speed");

        private const float IdleAnimValue = 0f;
        private const float WalkAnimValue = 0.5f;
        private const float RunAnimValue = 1f;

        [Header("Input")]
        [SerializeField] private PlayerInputReader _input;

        [Header("Movement")]
        [SerializeField] private float _walkSpeed = 3f;
        [SerializeField] private float _runSpeed = 6f;
        [SerializeField] private float _acceleration = 60f;

        [Header("Camera")]
        [Tooltip("Camera dùng để tính hướng di chuyển relative. Để trống = Camera.main.")]
        [SerializeField] private Camera _camera;

        [Header("Animation (optional)")]
        [SerializeField] private Animator _animator;
        [SerializeField] private float _speedDampTime = 0.1f;

        [Header("Audio (optional)")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip[] _footstepClips;
        [SerializeField] private float _walkStepInterval = 0.45f;
        [SerializeField] private float _runStepInterval = 0.28f;
        [SerializeField][Range(0f, 1f)] private float _footstepVolume = 0.7f;

        private Rigidbody _rigidbody;
        private float _nextFootstepTime;

        public bool IsMoving { get; private set; }
        public bool IsRunning { get; private set; }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            ConfigureRigidbodyForTopDown();

            if (_camera == null)
                _camera = Camera.main;
        }

        private void ConfigureRigidbodyForTopDown()
        {
            _rigidbody.isKinematic = false;
            _rigidbody.useGravity = true;
            _rigidbody.constraints = RigidbodyConstraints.FreezeRotationX
                                   | RigidbodyConstraints.FreezeRotationZ;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        private void FixedUpdate()
        {
            Vector3 worldDirection = ReadMovementDirection();
            UpdateMovementFlags(worldDirection);

            float targetSpeed = IsRunning ? _runSpeed : _walkSpeed;
            ApplyHorizontalVelocity(worldDirection * targetSpeed);
        }

        private void Update()
        {
            DriveLocomotionAnimator();
            TickFootstep();
        }

        /// <summary>
        /// Đọc input 2D rồi chuyển thành hướng 3D RELATIVE theo camera.
        /// Camera top-down thường nghiêng góc X (vd 60°), nên forward của camera
        /// khi chiếu xuống mặt phẳng ngang mới là "hướng lên" thực sự.
        /// </summary>
        private Vector3 ReadMovementDirection()
        {
            Vector2 axis = _input.MovementAxis;
            if (axis.sqrMagnitude < 0.001f) return Vector3.zero;

            // Dùng góc Y (yaw) của camera — bỏ qua pitch (X=60°)
            // để "lên joystick" = đi về phía camera đang nhìn trên mặt phẳng ngang
            float camYaw = _camera.transform.eulerAngles.y;
            Quaternion flatR = Quaternion.Euler(0f, camYaw, 0f);
            Vector3 camForward = flatR * Vector3.forward;
            Vector3 camRight = flatR * Vector3.right;

            Vector3 direction = camForward * axis.y + camRight * axis.x;

            // Clamp magnitude về 1 (tránh diagonal nhanh hơn)
            if (direction.sqrMagnitude > 1f) direction.Normalize();

            return direction;
        }

        private void UpdateMovementFlags(Vector3 worldDirection)
        {
            const float minMovementSqr = 0.01f;
            IsMoving = worldDirection.sqrMagnitude > minMovementSqr;
            IsRunning = IsMoving && _input.SprintHeld;
        }

        private void ApplyHorizontalVelocity(Vector3 desiredVelocity)
        {
            Vector3 current = _rigidbody.linearVelocity;
            Vector3 horizontalCurrent = new Vector3(current.x, 0f, current.z);

            Vector3 nextHorizontal = Vector3.MoveTowards(
                horizontalCurrent,
                desiredVelocity,
                _acceleration * Time.fixedDeltaTime);

            _rigidbody.linearVelocity = new Vector3(nextHorizontal.x, current.y, nextHorizontal.z);
        }

        private void DriveLocomotionAnimator()
        {
            if (_animator == null) return;
            float target = ResolveAnimatorSpeedValue();
            _animator.SetFloat(SpeedParam, target, _speedDampTime, Time.deltaTime);
        }

        private float ResolveAnimatorSpeedValue()
        {
            if (!IsMoving) return IdleAnimValue;
            return IsRunning ? RunAnimValue : WalkAnimValue;
        }

        private void TickFootstep()
        {
            if (!CanPlayFootstep()) return;

            if (!IsMoving)
            {
                _nextFootstepTime = Time.time;
                return;
            }

            if (Time.time < _nextFootstepTime) return;

            PlayRandomFootstep();
            float interval = IsRunning ? _runStepInterval : _walkStepInterval;
            _nextFootstepTime = Time.time + interval;
        }

        private bool CanPlayFootstep()
        {
            return _audioSource != null
                && _footstepClips != null
                && _footstepClips.Length > 0;
        }

        private void PlayRandomFootstep()
        {
            AudioClip clip = _footstepClips[Random.Range(0, _footstepClips.Length)];
            if (clip == null) return;
            _audioSource.PlayOneShot(clip, _footstepVolume);
        }
    }
}