using UnityEngine;

namespace SimpleSurvival.Cameras
{
    public class CameraRigController : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Transform mà camera sẽ follow. Thường là Player root.")]
        [SerializeField] private Transform target;

        [Header("Follow Settings")]
        [Tooltip("Độ mượt khi follow. Cao = nhanh bám target. Thấp = trễ, mượt. Khuyến nghị: 8-12.")]
        [SerializeField, Range(1f, 30f)] private float followSmoothness = 10f;

        [Tooltip("Chiều cao Y cố định của rig (rig không follow trục Y của target).")]
        [SerializeField] private float rigHeight = 0.75f;

        [Header("Debug Info (read-only)")]
        [Tooltip("Yaw angle hiện tại của rig — input layer sẽ đọc giá trị này.")]
        [SerializeField] private float yawAngle = 45f;

        public float YawAngle => yawAngle;

        public bool HasTarget => target != null;

        private void LateUpdate()
        {
            if (target == null) return;

            FollowTarget();
        }
        private void FollowTarget()
        {
            Vector3 targetPos = target.position;
            Vector3 desiredPos = new Vector3(targetPos.x, rigHeight, targetPos.z);
            transform.position = Vector3.Lerp(
                transform.position,
                desiredPos,
                followSmoothness * Time.deltaTime
            );
        }

        public void Snap()
        {
            if (target == null) return;
            Vector3 t = target.position;
            transform.position = new Vector3(t.x, rigHeight, t.z);
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