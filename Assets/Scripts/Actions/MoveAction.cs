using System;
using UnityEngine;
using SimpleSurvival.Player;

namespace SimpleSurvival.Actions
{
    public class MoveAction : IAction
    {
        public ActionType Type => ActionType.Move;
        public bool IsCompleted => _inputMagnitude < 0.1f;
        public event Action<IAction> Completed;

        public bool IsRunning { get; private set; }
        public bool IsSneaking { get; private set; }
        public float NormalizedSpeed { get; private set; }

        private readonly PlayerActionController _controller;
        private readonly CharacterController _cc;

        private Vector3 _inputDirection;
        private float _inputMagnitude;
        private bool _sneakHeld;

        private Vector3 _horizontalVelocity;
        private float _verticalVelocity;

        private float _walkSpeed;
        private float _runSpeed;
        private float _sneakSpeed;
        private float _runThreshold;
        private float _acceleration;
        private float _rotationSmoothness;
        private float _gravity;

        private float _originalHeight;
        private float _originalCenterY;
        private float _sneakHeightReduction;
        private float _sneakLerpSpeed;
        private LayerMask _standUpCheckMask;
        private bool _heightCaptured;

        public MoveAction(PlayerActionController controller, MoveActionConfig config)
        {
            _controller = controller;
            _cc = controller.Controller;

            _walkSpeed = config.walkSpeed;
            _runSpeed = config.runSpeed;
            _sneakSpeed = config.sneakSpeed;
            _runThreshold = config.runThreshold;
            _acceleration = config.acceleration;
            _rotationSmoothness = config.rotationSmoothness;
            _gravity = config.gravity;

            _sneakHeightReduction = config.sneakHeightReduction;
            _sneakLerpSpeed = config.sneakLerpSpeed;
            _standUpCheckMask = config.standUpCheckMask;
        }

        public bool CanBeInterruptedBy(IAction newAction) => true;

        public void Init()
        {
            CaptureHeightIfNeeded();
        }

        public void UpdateInput(Vector3 worldDirection, float magnitude, bool sneakHeld)
        {
            _inputDirection = worldDirection;
            _inputMagnitude = magnitude;
            _sneakHeld = sneakHeld;
        }

        public void Update(float deltaTime)
        {
            CaptureHeightIfNeeded();
            UpdateSneakState();
            ApplySneakCollider(deltaTime);
            UpdateMovement(deltaTime);
            UpdateRotation(deltaTime);
        }

        public void Cancel()
        {
            _horizontalVelocity = Vector3.zero;
        }

        private void CaptureHeightIfNeeded()
        {
            if (_heightCaptured) return;
            if (_cc.height < 0.01f) return;

            _originalHeight = _cc.height;
            _originalCenterY = _cc.center.y;
            _heightCaptured = true;
        }

        private void UpdateSneakState()
        {
            bool wantSneak = _sneakHeld;

            if (!wantSneak && IsSneaking && !CanStandUp())
            {
                IsSneaking = true;
                return;
            }

            IsSneaking = wantSneak;
        }

        private void ApplySneakCollider(float deltaTime)
        {
            if (!_heightCaptured) return;

            float targetHeight = IsSneaking
                ? _originalHeight - _sneakHeightReduction
                : _originalHeight;

            if (Mathf.Abs(_cc.height - targetHeight) < 0.001f) return;

            float prevHeight = _cc.height;
            _cc.height = Mathf.Lerp(_cc.height, targetHeight, _sneakLerpSpeed * deltaTime);

            float delta = _cc.height - prevHeight;
            _cc.center = new Vector3(_cc.center.x, _cc.center.y + delta * 0.5f, _cc.center.z);
        }

        private bool CanStandUp()
        {
            float radius = _cc.radius * 0.9f;
            Vector3 origin = _controller.PlayerTransform.position + Vector3.up * (_cc.height + 0.05f);
            return !Physics.SphereCast(
                origin, radius, Vector3.up, out _,
                _sneakHeightReduction, _standUpCheckMask, QueryTriggerInteraction.Ignore
            );
        }

        private void UpdateMovement(float deltaTime)
        {
            bool isMoving = _inputMagnitude > 0.1f;
            IsRunning = isMoving && !IsSneaking && _inputMagnitude >= _runThreshold;

            float targetSpeed = IsSneaking ? _sneakSpeed
                              : IsRunning ? _runSpeed
                              : _walkSpeed;

            Vector3 desiredVelocity = isMoving ? _inputDirection * targetSpeed : Vector3.zero;

            _horizontalVelocity = Vector3.MoveTowards(
                _horizontalVelocity, desiredVelocity, _acceleration * deltaTime
            );

            if (_cc.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;
            else
                _verticalVelocity += _gravity * deltaTime;

            Vector3 totalVelocity = _horizontalVelocity + Vector3.up * _verticalVelocity;
            _cc.Move(totalVelocity * deltaTime);

            if (!isMoving)
                NormalizedSpeed = 0f;
            else if (IsRunning)
                NormalizedSpeed = 1f;
            else
                NormalizedSpeed = 0.3f;
        }

        private void UpdateRotation(float deltaTime)
        {
            if (_inputMagnitude < 0.1f) return;

            Quaternion targetRot = Quaternion.LookRotation(_inputDirection, Vector3.up);
            _controller.PlayerTransform.rotation = Quaternion.Slerp(
                _controller.PlayerTransform.rotation, targetRot, _rotationSmoothness * deltaTime
            );
        }
    }
}