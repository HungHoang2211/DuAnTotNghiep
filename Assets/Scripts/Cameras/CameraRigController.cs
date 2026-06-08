using UnityEngine;

namespace SimpleSurvival.Cameras
{
    public class CameraRigController : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Follow Settings")]
        [SerializeField, Range(0.01f, 1f)] private float followSmoothTime = 0.1f;
        [SerializeField] private float rigHeight = 0.75f;
        [SerializeField] private float snapDistance = 5f;

        [Header("Debug Info")]
        [SerializeField] private float yawAngle = 45f;

        public float YawAngle => yawAngle;
        public bool HasTarget => target != null;

        private Vector3 _followVelocity = Vector3.zero;

        private void LateUpdate()
        {
            if (target == null) return;
            FollowTarget();
        }

        private void FollowTarget()
        {
            Vector3 targetPos = target.position;
            Vector3 desiredPos = new Vector3(targetPos.x, rigHeight, targetPos.z);

            float distance = Vector3.Distance(transform.position, desiredPos);

            if (distance > snapDistance)
            {
                transform.position = desiredPos;
                _followVelocity = Vector3.zero;
            }
            else
            {
                transform.position = Vector3.SmoothDamp(
                    transform.position,
                    desiredPos,
                    ref _followVelocity,
                    followSmoothTime
                );
            }
        }

        public void Snap()
        {
            if (target == null) return;
            Vector3 t = target.position;
            transform.position = new Vector3(t.x, rigHeight, t.z);
            _followVelocity = Vector3.zero;
        }

        public void SetTarget(Transform newTarget, bool snapImmediately = true)
        {
            target = newTarget;
            if (snapImmediately) Snap();
        }

        private void OnValidate()
        {
            yawAngle = transform.eulerAngles.y;
        }

        [ContextMenu("Test Snap to Target")]
        private void TestSnap()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[CameraRig] Snap chỉ test khi đang Play.");
                return;
            }
            Snap();
            Debug.Log($"[CameraRig] Snapped to {target?.name} at {transform.position}");
        }
    }
}