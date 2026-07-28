using UnityEngine;

namespace SimpleSurvival.Cameras
{
    public class CameraRigController : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Follow Settings")]
        [SerializeField, Range(0.01f, 1f)] private float followSmoothTime = 0.1f;
        [SerializeField] private float snapDistance = 5f;

        [Header("Height (Zoom)")]
        [SerializeField] private float normalHeight = 0.75f;
        [SerializeField] private float buildDefaultHeight = 10f;
        [SerializeField] private float minZoomHeight = 2f;
        [SerializeField] private float maxZoomHeight = 12f;

        [Header("Pitch")]
        [SerializeField] private float normalPitchAngle = 55f;
        [SerializeField] private float buildPitchAngle = 55f;
        [SerializeField] private float pitchLerpSpeed = 5f;

        [Header("Debug Info")]
        [SerializeField] private float yawAngle = 45f;

        public float YawAngle => yawAngle;
        public bool HasTarget => target != null;

        private Vector3 followVelocity = Vector3.zero;
        private float targetPitchAngle;
        private float freeHeight;
        private Vector2 freeAimXZ;
        private bool isBuildMode;

        private void Awake()
        {
            targetPitchAngle = normalPitchAngle;
            freeHeight = buildDefaultHeight;
            freeAimXZ = new Vector2(transform.position.x, transform.position.z);
        }

        private void LateUpdate()
        {
            if (target != null)
                FollowTarget();

            UpdatePitch();
        }

        private void FollowTarget()
        {
            Vector3 targetPos = target.position;
            float height = isBuildMode ? buildDefaultHeight : normalHeight;
            Vector3 aimPoint = new Vector3(targetPos.x, height, targetPos.z);
            Vector3 desiredPos = isBuildMode ? ApplyAimOffset(aimPoint, height) : aimPoint;

            float distance = Vector3.Distance(transform.position, desiredPos);

            if (distance > snapDistance)
            {
                transform.position = desiredPos;
                followVelocity = Vector3.zero;
            }
            else
            {
                transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref followVelocity, followSmoothTime);
            }
        }

        private void UpdatePitch()
        {
            float currentPitch = transform.eulerAngles.x;
            float newPitch = Mathf.LerpAngle(currentPitch, targetPitchAngle, pitchLerpSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(newPitch, yawAngle, 0f);
        }

        private Vector3 ApplyAimOffset(Vector3 aimPoint, float height)
        {
            float pitchRad = targetPitchAngle * Mathf.Deg2Rad;
            if (pitchRad <= 0.01f) return aimPoint;

            float pullBack = height / Mathf.Tan(pitchRad);
            Vector3 forward = Quaternion.Euler(0f, yawAngle, 0f) * Vector3.forward;
            return aimPoint - forward * pullBack;
        }

        public void SetBuildMode(bool buildMode)
        {
            isBuildMode = buildMode;
            targetPitchAngle = buildMode ? buildPitchAngle : normalPitchAngle;
            if (buildMode) freeHeight = buildDefaultHeight;
        }

        public void Snap()
        {
            if (target == null) return;
            float height = isBuildMode ? buildDefaultHeight : normalHeight;
            Vector3 aimPoint = new Vector3(target.position.x, height, target.position.z);
            transform.position = isBuildMode ? ApplyAimOffset(aimPoint, height) : aimPoint;
            followVelocity = Vector3.zero;
        }

        public void SetTarget(Transform newTarget, bool snapImmediately = true)
        {
            target = newTarget;
            if (snapImmediately) Snap();
        }

        public void ClearTarget()
        {
            freeAimXZ = target != null
                ? new Vector2(target.position.x, target.position.z)
                : new Vector2(transform.position.x, transform.position.z);

            target = null;
            ApplyFreePosition();
        }

        public void SetFreePosition(Vector2 worldXZ)
        {
            if (target != null) return;
            freeAimXZ = worldXZ;
            ApplyFreePosition();
        }

        public void AdjustFreeHeight(float delta)
        {
            freeHeight = Mathf.Clamp(freeHeight + delta, minZoomHeight, maxZoomHeight);
            if (target == null) ApplyFreePosition();
        }

        private void ApplyFreePosition()
        {
            Vector3 aimPoint = new Vector3(freeAimXZ.x, freeHeight, freeAimXZ.y);
            transform.position = isBuildMode ? ApplyAimOffset(aimPoint, freeHeight) : aimPoint;
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