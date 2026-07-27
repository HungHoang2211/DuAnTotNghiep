using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SimpleSurvival.Cameras
{
    public sealed class BuildCameraPanZoomSurface : MonoBehaviour,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private CameraRigController cameraRig;

        [Header("Pan")]
        [SerializeField] private float panSpeed = 0.02f;
        [Tooltip("Nếu gán, dùng bounds của Collider này thay cho 2 giá trị bên dưới.")]
        [SerializeField] private Collider panBoundsCollider;
        [SerializeField] private Vector2 panBoundsMin = new Vector2(-16f, -16f);
        [SerializeField] private Vector2 panBoundsMax = new Vector2(16f, 16f);

        [Header("Zoom")]
        [SerializeField] private float zoomSpeed = 0.02f;
        [SerializeField] private float minZoomDistance = 25f;
        [SerializeField] private float maxZoomDistance = 45f;

        private readonly Dictionary<int, Vector2> activePointers = new Dictionary<int, Vector2>();
        private float lastPinchDistance;

        public void OnPointerDown(PointerEventData eventData)
        {
            activePointers[eventData.pointerId] = eventData.position;

            if (activePointers.Count == 2)
                lastPinchDistance = GetCurrentPinchDistance();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (cameraRig == null || cameraRig.HasTarget) return;

            activePointers[eventData.pointerId] = eventData.position;

            if (activePointers.Count >= 2)
                HandlePinch();
            else
                HandlePan(eventData.delta);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            activePointers.Remove(eventData.pointerId);
        }

        private void HandlePan(Vector2 delta)
        {
            Quaternion yawRotation = Quaternion.Euler(0f, cameraRig.YawAngle, 0f);
            Vector3 right = yawRotation * Vector3.right;
            Vector3 forward = yawRotation * Vector3.forward;
            Vector3 move = (-delta.x * right + -delta.y * forward) * panSpeed;

            Vector3 newPos = cameraRig.transform.position + move;
            Vector2 min = GetBoundsMin();
            Vector2 max = GetBoundsMax();
            newPos.x = Mathf.Clamp(newPos.x, min.x, max.x);
            newPos.z = Mathf.Clamp(newPos.z, min.y, max.y);

            cameraRig.SetFreePosition(newPos);
        }

        private void HandlePinch()
        {
            float currentDistance = GetCurrentPinchDistance();
            float delta = currentDistance - lastPinchDistance;
            lastPinchDistance = currentDistance;

            cameraRig.AdjustFreeDistance(-delta * zoomSpeed, minZoomDistance, maxZoomDistance);
        }

        private float GetCurrentPinchDistance()
        {
            Dictionary<int, Vector2>.Enumerator e = activePointers.GetEnumerator();
            e.MoveNext();
            Vector2 a = e.Current.Value;
            e.MoveNext();
            Vector2 b = e.Current.Value;
            return Vector2.Distance(a, b);
        }

        private Vector2 GetBoundsMin()
        {
            if (panBoundsCollider != null)
            {
                Bounds b = panBoundsCollider.bounds;
                return new Vector2(b.min.x, b.min.z);
            }
            return panBoundsMin;
        }

        private Vector2 GetBoundsMax()
        {
            if (panBoundsCollider != null)
            {
                Bounds b = panBoundsCollider.bounds;
                return new Vector2(b.max.x, b.max.z);
            }
            return panBoundsMax;
        }
    }
}