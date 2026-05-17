using UnityEngine;

namespace Xyla.Player
{
    public class TopDownCameraFollow : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform _target;

        [Tooltip("Rigidbody của Player. Để trống → script tự tìm trên _target. " +
                 "Cần thiết để fix camera rung do physics timestep.")]
        [SerializeField] private Rigidbody _targetRigidbody;

        [Header("Distance")]
        [Tooltip("Khoảng cách camera-player khi ở vùng trống (zoom out).")]
        [SerializeField] private float _maxDistance = 15f;

        [Tooltip("Khoảng cách camera-player khi sát vật cản (zoom in).")]
        [SerializeField] private float _minDistance = 8f;

        [Tooltip("Thời gian (giây) để zoom về đích. Càng lớn càng mượt.")]
        [SerializeField] private float _zoomSmoothTime = 0.4f;

        [Header("Proximity Detection")]
        [Tooltip("Bán kính quét quanh player để tìm obstacle gần nhất.")]
        [SerializeField] private float _proximityRadius = 8f;

        [Tooltip("Layer của obstacle (tường, prop). Không include Player và Ground.")]
        [SerializeField] private LayerMask _obstacleLayers;

        [Header("Follow")]
        [Tooltip("Thời gian (giây) để camera đuổi kịp player. 0.08–0.15 là khoảng dễ chịu.")]
        [SerializeField] private float _followSmoothTime = 0.1f;

        private const int ObstacleBufferSize = 16;
        private readonly Collider[] _obstacleBuffer = new Collider[ObstacleBufferSize];

        private float _currentDistance;
        private float _zoomVelocity;
        private Vector3 _positionVelocity;

        public void SetTarget(Transform target)
        {
            _target = target;
            _targetRigidbody = target != null ? target.GetComponent<Rigidbody>() : null;
        }


        private void Start()
        {
            if (_target == null) return;

            if (_targetRigidbody == null)
                _targetRigidbody = _target.GetComponent<Rigidbody>();

            _currentDistance = _maxDistance;
            transform.position = ComputeDesiredPosition();
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            UpdateDistanceBasedOnProximity();
            FollowTargetSmoothly();
        }


        private void UpdateDistanceBasedOnProximity()
        {
            float closestObstacle = FindClosestObstacleDistance();
            float openness = Mathf.Clamp01(closestObstacle / _proximityRadius);
            float desiredDistance = Mathf.Lerp(_minDistance, _maxDistance, openness);

            _currentDistance = Mathf.SmoothDamp(
                _currentDistance,
                desiredDistance,
                ref _zoomVelocity,
                _zoomSmoothTime);
        }

        private float FindClosestObstacleDistance()
        {
            Vector3 origin = GetPhysicsPosition();
            int hitCount = Physics.OverlapSphereNonAlloc(
                origin,
                _proximityRadius,
                _obstacleBuffer,
                _obstacleLayers,
                QueryTriggerInteraction.Ignore);

            float closest = _proximityRadius;
            for (int i = 0; i < hitCount; i++)
            {
                if (BelongsToTarget(_obstacleBuffer[i])) continue;
                Vector3 nearestPoint = _obstacleBuffer[i].ClosestPoint(origin);
                float distance = Vector3.Distance(origin, nearestPoint);
                if (distance < closest) closest = distance;
            }
            return closest;
        }

        private bool BelongsToTarget(Collider candidate)
        {
            Transform t = candidate.transform;
            return t == _target || t.IsChildOf(_target);
        }


        private void FollowTargetSmoothly()
        {
            Vector3 desired = ComputeDesiredPosition();
            transform.position = Vector3.SmoothDamp(
                transform.position,
                desired,
                ref _positionVelocity,
                _followSmoothTime);
        }

        private Vector3 GetPhysicsPosition()
        {
            return _targetRigidbody != null
                ? _targetRigidbody.position
                : _target.position;
        }

        private Vector3 ComputeDesiredPosition()
        {
            return GetPhysicsPosition() - transform.forward * _currentDistance;
        }

        private void OnDrawGizmosSelected()
        {
            if (_target == null) return;
            Gizmos.color = new Color(1f, 0.7f, 0.2f, 0.3f);
            Gizmos.DrawWireSphere(_target.position, _proximityRadius);
        }
    }
}