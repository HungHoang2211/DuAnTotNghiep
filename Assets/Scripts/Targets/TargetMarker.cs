using UnityEngine;

namespace SimpleSurvival.Targets
{
    public class TargetMarker : MonoBehaviour
    {
        [SerializeField] private float yOffset = 0.05f;
        [SerializeField] private bool autoScaleByRadius = true;

        private Transform _followTarget;
        private float _currentRadius;

        private void Awake()
        {
            gameObject.SetActive(false);
        }

        public void Show(Transform target, float radius)
        {
            _followTarget = target;
            _currentRadius = radius;
            ApplyTransform();
            gameObject.SetActive(true);
        }

        public void Show(Vector3 position, float radius)
        {
            _followTarget = null;
            _currentRadius = radius;
            transform.position = new Vector3(position.x, yOffset, position.z);

            if (autoScaleByRadius)
                transform.localScale = Vector3.one * (radius * 2f);

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            _followTarget = null;
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (_followTarget == null) return;
            ApplyTransform();
        }

        private void ApplyTransform()
        {
            if (_followTarget == null) return;

            Vector3 pos = _followTarget.position;
            Debug.Log($"[Marker] target={_followTarget.name}, target.position={pos}, applied to {gameObject.name}");

            pos.y = yOffset;
            transform.position = pos;

            if (autoScaleByRadius)
                transform.localScale = Vector3.one * (_currentRadius * 2f);
        }
    }
}