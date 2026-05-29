using UnityEngine;

namespace Xyla.Player
{
    /// Camera follow top-down theo kiến trúc 2 transform:
    ///   CameraRig (script này) → bám player, giữ rotation cố định
    ///   Camera (child)        → offset theo trục Z local
    ///
    /// SETUP HIERARCHY:
    ///   CameraRig (GameObject rỗng) ← script này
    ///     └── Main Camera           ← kéo vào _cameraTransform
    ///
    /// Rotation của CameraRig set trong Inspector (vd X=45, Y=45, Z=0).
    /// Script chỉ điều khiển position của CameraRig và Z offset của Camera.
    public class TopDownCameraFollow : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform _target;

        [Header("Camera Child")]
        [Tooltip("Transform của Camera con. Kéo Main Camera vào đây.")]
        [SerializeField] private Transform _cameraTransform;

        [Header("Follow")]
        [Tooltip("Độ cao CameraRig so với mặt đất.")]
        [SerializeField] private float _cameraHeight = 0.75f;

        [Tooltip("Tốc độ bám player (lerp speed).")]
        [SerializeField] private float _followSpeed = 10f;

        [Header("Distance")]
        [Tooltip("Khoảng cách camera-pivot (Z offset của camera child).")]
        [SerializeField] private float _cameraDistance = 12f;

        [Tooltip("Tốc độ lerp distance.")]
        [SerializeField] private float _distanceSpeed = 5f;

        public void SetTarget(Transform target) => _target = target;

        private void Start()
        {
            if (_target == null || _cameraTransform == null) return;
            // Snap ngay lập tức lúc đầu
            Vector3 rigPos = new Vector3(_target.position.x, _cameraHeight, _target.position.z);
            transform.position = rigPos;
            _cameraTransform.localPosition = new Vector3(0f, 0f, -_cameraDistance);
        }

        private Vector3 _positionVelocity;

        private void LateUpdate()
        {
            if (_target == null || _cameraTransform == null) return;

            // CameraRig bám theo player — dùng SmoothDamp thay vì Lerp để không rung
            Vector3 desiredPos = new Vector3(
                _target.position.x,
                _cameraHeight,
                _target.position.z);

            transform.position = Vector3.SmoothDamp(
                transform.position, desiredPos,
                ref _positionVelocity,
                1f / _followSpeed);

            // Camera child lerp về đúng distance theo trục Z local
            float desiredZ = -_cameraDistance;
            float currentZ = _cameraTransform.localPosition.z;
            float newZ = Mathf.Lerp(currentZ, desiredZ, Time.deltaTime * _distanceSpeed);
            _cameraTransform.localPosition = new Vector3(0f, 0f, newZ);
        }
    }
}