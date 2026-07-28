using System.Collections.Generic;
using SimpleSurvival.Building;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SimpleSurvival.Cameras
{
    public sealed class BuildCameraPanZoomSurface : MonoBehaviour,
        IPointerDownHandler, IDragHandler, IPointerUpHandler, IScrollHandler, IPointerClickHandler
    {
        [SerializeField] private CameraRigController cameraRig;

        [Header("Pan")]
        [SerializeField] private float panSpeed = 0.02f;
        [Tooltip("Nếu gán, dùng bounds của Collider này thay cho 2 giá trị bên dưới.")]
        [SerializeField] private Collider panBoundsCollider;
        [SerializeField] private Vector2 panBoundsMin = new Vector2(-16f, -16f);
        [SerializeField] private Vector2 panBoundsMax = new Vector2(16f, 16f);

        [Header("Zoom (Height)")]
        [SerializeField] private float zoomSpeed = 0.01f;

        private readonly Dictionary<int, Vector2> activePointers = new Dictionary<int, Vector2>();
        private Vector2 currentFreePosition;
        private float lastPinchDistance;

        private void OnEnable()
        {
            if (cameraRig != null)
                currentFreePosition = new Vector2(cameraRig.transform.position.x, cameraRig.transform.position.z);
        }

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

        public void OnPointerClick(PointerEventData eventData)
        {
            BuildModeController.Instance?.HandleWorldTap(eventData.position);
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (cameraRig == null || cameraRig.HasTarget) return;

            cameraRig.AdjustFreeHeight(-eventData.scrollDelta.y * zoomSpeed * 5f);
        }

        private void HandlePan(Vector2 delta)
        {
            Quaternion yawRotation = Quaternion.Euler(0f, cameraRig.YawAngle, 0f);
            Vector3 right = yawRotation * Vector3.right;
            Vector3 forward = yawRotation * Vector3.forward;
            Vector3 move = (-delta.x * right + -delta.y * forward) * panSpeed;

            currentFreePosition += new Vector2(move.x, move.z);

            Vector2 min = GetBoundsMin();
            Vector2 max = GetBoundsMax();
            currentFreePosition.x = Mathf.Clamp(currentFreePosition.x, min.x, max.x);
            currentFreePosition.y = Mathf.Clamp(currentFreePosition.y, min.y, max.y);

            cameraRig.SetFreePosition(currentFreePosition);
        }

        private void HandlePinch()
        {
            float currentDistance = GetCurrentPinchDistance();
            float delta = currentDistance - lastPinchDistance;
            lastPinchDistance = currentDistance;

            cameraRig.AdjustFreeHeight(-delta * zoomSpeed);
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