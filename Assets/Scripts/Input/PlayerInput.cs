using SimpleSurvival.Cameras;
using UnityEngine;

namespace SimpleSurvival.Input
{
    /// <summary>
    /// Bridge giữa joystick (screen space 2D) và player movement (world space 3D).
    /// 
    /// Trách nhiệm CHÍNH: xoay direction từ screen space sang world space theo camera yaw.
    /// 
    /// Đây là chỗ ĐÚNG để bù camera angle 45° — KHÔNG xoay model, KHÔNG xoay camera bù.
    /// LDOE làm đúng chỗ này (xem DpadController.OnSwipe).
    /// 
    /// Gắn lên Player GameObject.
    /// </summary>
    public class PlayerInput : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Joystick UI source. Kéo GameObject Joystick_Background vào.")]
        [SerializeField] private VirtualJoystick joystick;

        [Tooltip("Camera rig để đọc yaw angle. Tự tìm nếu để trống.")]
        [SerializeField] private CameraRigController cameraRig;

        [Header("Settings")]
        [Tooltip("Cho phép input từ keyboard WASD để test trên editor.")]
        [SerializeField] private bool enableKeyboardFallback = true;

        // ========== Public output cho PlayerMovement đọc ==========

        /// <summary>
        /// Direction trong WORLD space, đã bù camera angle. Luôn nằm trên mặt phẳng XZ (Y=0).
        /// Đây là direction nhân vật sẽ di chuyển.
        /// </summary>
        public Vector3 WorldDirection { get; private set; } = Vector3.zero;

        /// <summary>Magnitude 0..1, dùng cho analog speed control.</summary>
        public float Magnitude { get; private set; } = 0f;

        /// <summary>True nếu có input (joystick đang được kéo hoặc keyboard đang nhấn).</summary>
        public bool HasInput => Magnitude > 0.01f;

        private void Awake()
        {
            if (cameraRig == null)
                cameraRig = FindObjectOfType<CameraRigController>();
        }

        private void Update()
        {
            // Đọc raw input từ joystick HOẶC keyboard
            Vector2 rawInput = ReadRawInput();

            if (rawInput.sqrMagnitude < 0.001f)
            {
                WorldDirection = Vector3.zero;
                Magnitude = 0f;
                return;
            }

            // === ĐÂY LÀ CHỖ QUAN TRỌNG NHẤT ===
            // Joystick "lên" (0, 1) trong screen space cần map sang "lên trong view camera" trong world.
            // Camera xoay yaw 45° quanh Y, nên direction trong world phải xoay BÙ 45°.
            // 
            // Sai lầm phổ biến (nhóm bạn đang làm): KHÔNG xoay ở đây → joystick "lên" map ra +Z world
            //   → để có cảm giác isometric, lại đi xoay MODEL 45° → mọi hệ thống dùng forward của
            //   model đều sai → bug lan tỏa.
            //
            // Đúng: xoay direction Ở ĐÂY (input layer), model giữ forward = +Z chuẩn.

            float yaw = cameraRig != null ? cameraRig.YawAngle : 45f;
            Vector3 inputAsWorld = new Vector3(rawInput.x, 0f, rawInput.y);
            WorldDirection = Quaternion.Euler(0f, yaw, 0f) * inputAsWorld;

            Magnitude = Mathf.Clamp01(rawInput.magnitude);

            // Normalize WorldDirection để direction là pure direction (magnitude xử lý riêng)
            if (WorldDirection.sqrMagnitude > 0.001f)
                WorldDirection = WorldDirection.normalized;
        }

        private Vector2 ReadRawInput()
        {
            // Ưu tiên joystick nếu đang được nhấn
            if (joystick != null && joystick.IsPressed)
                return joystick.Direction * joystick.Magnitude;

            // Fallback: WASD trên editor để test nhanh
            if (enableKeyboardFallback)
            {
                float h = UnityEngine.Input.GetAxisRaw("Horizontal");
                float v = UnityEngine.Input.GetAxisRaw("Vertical");
                Vector2 kb = new Vector2(h, v);
                if (kb.sqrMagnitude > 1f) kb.Normalize();
                return kb;
            }

            return Vector2.zero;
        }
    }
}