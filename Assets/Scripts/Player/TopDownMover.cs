using UnityEngine;

namespace Xyla.Player
{
    /// <summary>
    /// Di chuyển nhân vật theo trục thế giới dựa trên WASD.
    /// Dùng Rigidbody để va chạm với object/tường tự nhiên.
    /// Hướng di chuyển ĐỘC LẬP với hướng quay của nhân vật (kiểu strafe).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class TopDownMover : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _acceleration = 60f;

        private Rigidbody _rigidbody;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            ConfigureRigidbodyForTopDown();
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
            Vector3 desiredVelocity = CalculateDesiredVelocity();
            ApplyHorizontalVelocity(desiredVelocity);
        }

        private Vector3 CalculateDesiredVelocity()
        {
            Vector2 axis = _input.MovementAxis;
            Vector3 worldDirection = new Vector3(axis.x, 0f, axis.y);
            if (worldDirection.sqrMagnitude > 1f) worldDirection.Normalize();
            return worldDirection * _moveSpeed;
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
    }
}
