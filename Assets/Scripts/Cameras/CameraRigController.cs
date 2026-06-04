using UnityEngine;

namespace SimpleSurvival.Cameras
{
    /// <summary>
    /// Camera rig 2-cấp, follow một Transform target.
    /// Gắn script này lên GameObject "CameraRig" (cha).
    /// Camera thật là con của CameraRig, không cần script riêng.
    /// 
    /// Trách nhiệm:
    /// - Theo dõi target mượt mà (lerp theo X/Z, giữ Y cố định).
    /// - Cung cấp Snap() để teleport tức thì (switch scene, respawn).
    /// - Cung cấp SetTarget() để đổi target runtime.
    /// - Expose YawAngle cho input layer dùng để map joystick → world direction.
    /// </summary>
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

        // ========== Public API ==========

        /// <summary>
        /// Góc xoay Yaw (quanh trục Y) của rig.
        /// Input layer dùng giá trị này để map joystick direction từ screen space sang world space.
        /// Ví dụ: nếu yaw = 45°, joystick "lên" sẽ map ra direction (sin45, 0, cos45) trong world.
        /// </summary>
        public float YawAngle => yawAngle;

        /// <summary>True nếu có target hợp lệ để follow.</summary>
        public bool HasTarget => target != null;

        // ========== Unity callbacks ==========

        private void LateUpdate()
        {
            // LateUpdate đảm bảo camera follow SAU KHI target đã di chuyển trong frame.
            // Nếu follow trong Update, camera có thể bị trễ 1 frame so với player → cảm giác giật.
            if (target == null) return;

            FollowTarget();
        }

        // ========== Core follow logic ==========

        private void FollowTarget()
        {
            Vector3 targetPos = target.position;

            // Chỉ follow X và Z. Y giữ cố định ở rigHeight.
            // Lý do: nếu player nhảy hoặc đi xuống dốc, camera không nên dập dềnh theo.
            Vector3 desiredPos = new Vector3(targetPos.x, rigHeight, targetPos.z);

            // Lerp framerate-independent.
            // Với smoothness ≤ 20, công thức `smoothness * dt` đủ chính xác.
            // (Công thức chính xác hơn: 1 - Mathf.Exp(-smoothness * dt), nhưng overkill ở đây.)
            transform.position = Vector3.Lerp(
                transform.position,
                desiredPos,
                followSmoothness * Time.deltaTime
            );
        }

        // ========== Public methods ==========

        /// <summary>
        /// Snap rig về vị trí target ngay lập tức, không lerp.
        /// Gọi khi: switch scene, player respawn, teleport, vào nhà.
        /// </summary>
        public void Snap()
        {
            if (target == null) return;
            Vector3 t = target.position;
            transform.position = new Vector3(t.x, rigHeight, t.z);
        }

        /// <summary>
        /// Đổi target để follow runtime (cutscene, follow vehicle, v.v.).
        /// </summary>
        /// <param name="newTarget">Transform mới để follow. Null để dừng follow.</param>
        /// <param name="snapImmediately">True để nhảy ngay, false để lerp trượt sang target mới.</param>
        public void SetTarget(Transform newTarget, bool snapImmediately = true)
        {
            target = newTarget;
            if (snapImmediately) Snap();
        }

        // ========== Editor utilities ==========

        /// <summary>
        /// Đồng bộ yawAngle hiển thị trong Inspector với rotation thật của transform.
        /// Chỉ chạy khi giá trị trong Inspector thay đổi, không ảnh hưởng runtime.
        /// </summary>
        private void OnValidate()
        {
            yawAngle = transform.eulerAngles.y;
        }

        /// <summary>
        /// Cho phép test Snap() bằng context menu khi đang Play.
        /// Right-click vào component → "Test Snap to Target".
        /// </summary>
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