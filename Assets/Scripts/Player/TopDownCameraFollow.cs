using UnityEngine;

namespace Xyla.Player
{
    /// <summary>
    /// Camera follow top-down — distance cố định, không zoom.
    /// _verticalOffset dịch điểm nhìn lên/xuống để player nằm đúng vị trí màn hình.
    /// </summary>
    public class TopDownCameraFollow : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform _target;

        [Header("Distance")]
        [SerializeField] private float _distance = 15f;

        [Tooltip("Dịch điểm camera nhìn theo trục Y. Tăng = player xuống thấp hơn trên màn hình.")]
        [SerializeField] private float _verticalOffset = 2f;

        [Header("Follow")]
        [SerializeField] private float _followSmoothTime = 0.1f;

        private Vector3 _positionVelocity;

        public void SetTarget(Transform target) => _target = target;

        private void Start()
        {
            if (_target == null) return;
            transform.position = ComputeDesiredPosition();
        }

        private void LateUpdate()
        {
            if (_target == null) return;
            FollowTargetSmoothly();
        }

        private void FollowTargetSmoothly()
        {
            Vector3 desired = ComputeDesiredPosition();
            transform.position = Vector3.SmoothDamp(
                transform.position, desired,
                ref _positionVelocity, _followSmoothTime);
        }

        private Vector3 ComputeDesiredPosition()
        {
            // Nhìn vào điểm cao hơn player một chút → player xuống thấp hơn trên màn hình
            Vector3 lookTarget = _target.position + Vector3.up * _verticalOffset;
            return lookTarget - transform.forward * _distance;
        }
    }
}