using UnityEngine;

namespace Xyla.Player
{
    [RequireComponent(typeof(CharacterController))]
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

        [Header("Gravity")]
        [SerializeField] private float _gravity = -20f;

        [Header("Animation (optional)")]
        [SerializeField] private Animator _animator;
        [SerializeField] private float _speedDampTime = 0.1f;

        [Header("Audio (optional)")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip[] _footstepClips;
        [SerializeField] private float _walkStepInterval = 0.45f;
        [SerializeField] private float _runStepInterval = 0.28f;
        [SerializeField][Range(0f, 1f)] private float _footstepVolume = 0.7f;

        private CharacterController _cc;
        private Vector3 _horizontalVelocity;
        private float _verticalVelocity;
        private float _nextFootstepTime;

        public bool IsMoving { get; private set; }
        public bool IsRunning { get; private set; }

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
        }

        private void Update()
        {
            Vector3 moveDir = ReadMovementDirection();
            UpdateMovementFlags(moveDir);

            float targetSpeed = IsRunning ? _runSpeed : _walkSpeed;
            Vector3 desiredHorizontal = moveDir * targetSpeed;

            // Smooth acceleration
            _horizontalVelocity = Vector3.MoveTowards(
                _horizontalVelocity,
                desiredHorizontal,
                _acceleration * Time.deltaTime);

            // Gravity
            if (_cc.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f; // giữ dính đất
            else
                _verticalVelocity += _gravity * Time.deltaTime;

            Vector3 finalVelocity = _horizontalVelocity + Vector3.up * _verticalVelocity;
            _cc.Move(finalVelocity * Time.deltaTime);

            DriveLocomotionAnimator();
            TickFootstep();
        }

        private Vector3 ReadMovementDirection()
        {
            Vector2 axis = _input.MovementAxis;
            Vector3 direction = new Vector3(axis.x, 0f, axis.y);
            if (direction.sqrMagnitude > 1f) direction.Normalize();
            return direction;
        }

        private void UpdateMovementFlags(Vector3 worldDirection)
        {
            const float minMovementSqr = 0.01f;
            IsMoving = worldDirection.sqrMagnitude > minMovementSqr;
            IsRunning = IsMoving && _input.SprintHeld;
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