using UnityEngine;

namespace SimpleSurvival.Cameras
{
    public sealed class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        [SerializeField] private Transform shakeTarget;

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
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static void Shake(float intensity, float duration)
        {
            if (Instance != null) Instance.StartShake(intensity, duration);
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