using System;
using UnityEngine;
using SimpleSurvival.Player;
using SimpleSurvival.Stats;
using SimpleSurvival.Targets;

namespace SimpleSurvival.Actions
{
    public class FollowAction : IAction, IMovingAction
    {
        public ActionType Type => ActionType.Follow;
        public bool IsCompleted { get; private set; }
        public event Action<IAction> Completed;

        public float NormalizedSpeed { get; private set; }

        private readonly PlayerActionController _controller;
        private readonly CharacterController _cc;
        private readonly PlayerStats _playerStats;
        private readonly ITargetable _target;
        private readonly float _arrivalRange;
        private readonly float _timeoutSeconds;
        private readonly Action _onArrived;

        private readonly float _walkMultiplier;
        private readonly float _acceleration;
        private readonly float _rotationSmoothness;
        private readonly float _gravity;

        private Vector3 _horizontalVelocity;
        private float _verticalVelocity;
        private float _elapsed;
        private bool _arrived;

        public FollowAction(
            PlayerActionController controller,
            MoveActionConfig config,
            PlayerStats playerStats,
            ITargetable target,
            float arrivalRange,
            float timeoutSeconds,
            Action onArrived)
        {
            _controller = controller;
            _cc = controller.Controller;
            _playerStats = playerStats;
            _target = target;
            _arrivalRange = arrivalRange;
            _timeoutSeconds = timeoutSeconds;
            _onArrived = onArrived;

            _walkMultiplier = config.walkMultiplier;
            _acceleration = config.acceleration;
            _rotationSmoothness = config.rotationSmoothness;
            _gravity = config.gravity;
        }

        public bool CanBeInterruptedBy(IAction newAction) => true;

        public void Init()
        {
            _elapsed = 0f;
            _arrived = false;
            IsCompleted = false;
        }

        public void Update(float deltaTime)
        {
            if (IsCompleted) return;

            if (_target == null || !_target.CanBeTargeted())
            {
                Complete();
                return;
            }

            _elapsed += deltaTime;
            if (_elapsed >= _timeoutSeconds)
            {
                Complete();
                return;
            }

            float distance = _controller.ComputeDistanceToTarget(_target);
            if (distance <= _arrivalRange)
            {
                _arrived = true;
                Complete();
                return;
            }

            MoveTowardsTarget(deltaTime);
        }

        private void MoveTowardsTarget(float deltaTime)
        {
            Vector3 toTarget = _target.Transform.position - _controller.PlayerTransform.position;
            toTarget.y = 0f;
            Vector3 direction = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector3.zero;

            float baseSpeed = _playerStats != null ? _playerStats.TotalMoveSpeed : 4f;
            float targetSpeed = baseSpeed * _walkMultiplier;

            Vector3 desiredVelocity = direction * targetSpeed;
            _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, desiredVelocity, _acceleration * deltaTime);

            if (_cc.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;
            else
                _verticalVelocity += _gravity * deltaTime;

            Vector3 totalVelocity = _horizontalVelocity + Vector3.up * _verticalVelocity;
            _cc.Move(totalVelocity * deltaTime);

            if (direction != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction, Vector3.up);
                _controller.PlayerTransform.rotation = Quaternion.Slerp(
                    _controller.PlayerTransform.rotation, targetRot, _rotationSmoothness * deltaTime);
            }

            NormalizedSpeed = 0.3f;
        }

        private void Complete()
        {
            NormalizedSpeed = 0f;
            IsCompleted = true;

            if (_arrived)
                _onArrived?.Invoke();

            Completed?.Invoke(this);
        }

        public void Cancel()
        {
            _horizontalVelocity = Vector3.zero;
            NormalizedSpeed = 0f;
        }
    }
}