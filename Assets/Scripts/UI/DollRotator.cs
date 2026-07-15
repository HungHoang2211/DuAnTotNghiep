using UnityEngine;
using UnityEngine.EventSystems;

namespace SimpleSurvival.UI
{
    public sealed class DollRotator : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Transform dollRoot;
        [SerializeField] private float rotateSpeed = 0.3f;
        [SerializeField] private float momentumDecay = 2f;
        [SerializeField] private bool autoReturnToDefault = true;
        [SerializeField] private float returnDelay = 1f;
        [SerializeField] private float returnSpeed = 90f;

        private float _defaultLocalYRotation;
        private float _angularVelocity;
        private bool _isDragging;
        private float _idleTimer;

        private void Awake()
        {
            if (dollRoot != null)
                _defaultLocalYRotation = dollRoot.localEulerAngles.y;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _isDragging = true;
            _angularVelocity = 0f;
            _idleTimer = 0f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (dollRoot == null) return;

            float deltaAngle = -eventData.delta.x * rotateSpeed;
            dollRoot.Rotate(Vector3.up, deltaAngle, Space.Self);
            _angularVelocity = deltaAngle;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _isDragging = false;
            _idleTimer = 0f;
        }

        private void Update()
        {
            if (dollRoot == null || _isDragging)
                return;

            if (Mathf.Abs(_angularVelocity) > 0.01f)
            {
                dollRoot.Rotate(Vector3.up, _angularVelocity, Space.Self);
                _angularVelocity = Mathf.MoveTowards(_angularVelocity, 0f, momentumDecay * Time.deltaTime);
                return;
            }

            if (!autoReturnToDefault)
                return;

            _idleTimer += Time.deltaTime;
            if (_idleTimer < returnDelay)
                return;

            Vector3 euler = dollRoot.localEulerAngles;
            euler.y = Mathf.MoveTowardsAngle(euler.y, _defaultLocalYRotation, returnSpeed * Time.deltaTime);
            dollRoot.localEulerAngles = euler;
        }
    }
}