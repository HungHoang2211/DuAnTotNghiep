using UnityEngine;

namespace SimpleSurvival.Cameras
{
    public sealed class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        [SerializeField] private Transform shakeTarget;

        [Header("Occlusion / Off-screen Check")]
        [Tooltip("Camera dùng để kiểm tra source có nằm trong khung hình không. Để trống sẽ tự lấy Camera.main.")]
        [SerializeField] private Camera viewCamera;

        [Tooltip("Lề an toàn quanh viewport (0 = đúng biên màn hình, 0.1 = cho phép lệch ra ngoài một chút vẫn tính là trong khung hình).")]
        [SerializeField, Range(0f, 0.5f)] private float viewportMargin = 0.05f;

        private Vector3 _lastOffset;
        private float _remainingDuration;
        private float _currentIntensity;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (shakeTarget == null) shakeTarget = transform;
            if (viewCamera == null) viewCamera = Camera.main;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Gây rung camera không điều kiện (không kiểm tra khung hình).
        /// Dùng cho các trường hợp rung không gắn với 1 nguồn cụ thể (vd: hit feedback trên player).
        /// </summary>
        public static void Shake(float intensity, float duration)
        {
            if (Instance != null) Instance.StartShake(intensity, duration);
        }

        /// <summary>
        /// Gây rung camera nhưng chỉ áp dụng nếu "source" (vd: transform của ZombieBoss)
        /// đang nằm trong khung hình camera. Nếu boss ở ngoài khung hình (offscreen) thì bỏ qua, không rung.
        /// </summary>
        public static void Shake(float intensity, float duration, Transform source)
        {
            if (Instance == null) return;
            if (source != null && !Instance.IsVisibleToCamera(source.position)) return;
            Instance.StartShake(intensity, duration);
        }

        public bool IsVisibleToCamera(Vector3 worldPosition)
        {
            if (viewCamera == null) viewCamera = Camera.main;
            if (viewCamera == null) return true; // không có camera để kiểm tra thì mặc định cho rung

            Vector3 viewportPoint = viewCamera.WorldToViewportPoint(worldPosition);

            // z <= 0 nghĩa là điểm nằm sau lưng camera
            if (viewportPoint.z <= 0f) return false;

            float min = -viewportMargin;
            float max = 1f + viewportMargin;

            return viewportPoint.x >= min && viewportPoint.x <= max
                && viewportPoint.y >= min && viewportPoint.y <= max;
        }

        public void StartShake(float intensity, float duration)
        {
            if (intensity > _currentIntensity)
                _currentIntensity = intensity;

            if (duration > _remainingDuration)
                _remainingDuration = duration;
        }

        private void LateUpdate()
        {
            if (shakeTarget == null) return;

            shakeTarget.localPosition -= _lastOffset;
            _lastOffset = Vector3.zero;

            if (_remainingDuration > 0f)
            {
                Vector2 offset = Random.insideUnitCircle * _currentIntensity;
                _lastOffset = new Vector3(offset.x, offset.y, 0f);
                shakeTarget.localPosition += _lastOffset;

                _remainingDuration -= Time.deltaTime;
                if (_remainingDuration <= 0f)
                {
                    _currentIntensity = 0f;
                }
            }
        }
    }
}