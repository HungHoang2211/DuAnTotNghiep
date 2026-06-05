using SimpleSurvival.Cameras;
using UnityEngine;

namespace SimpleSurvival.Input
{
    /// <summary>
    /// Abstraction layer giữa nguồn input (joystick/keyboard/button UI) và logic game.
    /// 
    /// Trách nhiệm:
    /// - Tổng hợp input từ NHIỀU nguồn: joystick mobile + keyboard PC + UI button.
    /// - Bù camera angle (xoay direction 45° để khớp isometric).
    /// - Expose API thống nhất cho PlayerMovement: WorldDirection, Magnitude, SneakHeld, SprintHeld.
    /// 
    /// PlayerMovement không quan tâm input đến từ đâu — chỉ đọc các property này.
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        [Header("Input Sources")]
        [Tooltip("Joystick UI cho mobile. Có thể null nếu chỉ test PC.")]
        [SerializeField] private VirtualJoystick joystick;

        [Tooltip("Camera rig để đọc yaw angle (bù camera bằng 45°).")]
        [SerializeField] private CameraRigController cameraRig;

        [Header("PC Keyboard Fallback")]
        [Tooltip("Cho phép keyboard input. Bật trong editor, có thể tắt khi build mobile.")]
        [SerializeField] private bool enableKeyboard = true;

        [Tooltip("Phím sprint (chạy). Mặc định Left Shift.")]
        [SerializeField] private KeyCode keyboardSprint = KeyCode.LeftShift;

        [Tooltip("Phím sneak. Mặc định Left Ctrl.")]
        [SerializeField] private KeyCode keyboardSneak = KeyCode.LeftControl;

        // ========== Movement (direction + magnitude) ==========

        /// <summary>Direction trong world space, đã bù camera angle. Y luôn = 0.</summary>
        public Vector3 WorldDirection { get; private set; } = Vector3.zero;

        /// <summary>Magnitude 0..1, dùng cho analog speed control.</summary>
        public float Magnitude { get; private set; } = 0f;

        /// <summary>True nếu có input di chuyển.</summary>
        public bool HasInput => Magnitude > 0.01f;

        // ========== Modifier buttons (sneak / sprint) ==========

        /// <summary>True nếu sneak đang được giữ (button UI hoặc keyboard).</summary>
        public bool SneakHeld { get; private set; }

        /// <summary>True nếu sprint/run đang được giữ.</summary>
        public bool SprintHeld { get; private set; }

        // External sources có thể set qua method (cho UI button)
        private bool _sneakFromUI = false;
        private bool _sprintFromUI = false;

        // ========== Public API cho UI button gọi ==========

        public void SetSneakFromUI(bool held) => _sneakFromUI = held;
        public void SetSprintFromUI(bool held) => _sprintFromUI = held;

        public void ForceSneak(bool value)
        {
            _sneakFromUI = value;
            SneakHeld = value;
        }

        private void Awake()
        {
            if (cameraRig == null)
                cameraRig = FindObjectOfType<CameraRigController>();
        }

        private void Update()
        {
            ReadMovement();
            ReadModifiers();
        }

        private void ReadMovement()
        {
            Vector2 rawInput = ReadRawMovementInput();

            if (rawInput.sqrMagnitude < 0.001f)
            {
                WorldDirection = Vector3.zero;
                Magnitude = 0f;
                return;
            }

            float yaw = cameraRig != null ? cameraRig.YawAngle : 45f;
            Vector3 inputAsWorld = new Vector3(rawInput.x, 0f, rawInput.y);
            WorldDirection = Quaternion.Euler(0f, yaw, 0f) * inputAsWorld;

            Magnitude = Mathf.Clamp01(rawInput.magnitude);

            if (WorldDirection.sqrMagnitude > 0.001f)
                WorldDirection = WorldDirection.normalized;
        }

        private Vector2 ReadRawMovementInput()
        {
            if (joystick != null && joystick.IsPressed)
                return joystick.Direction * joystick.Magnitude;

            if (enableKeyboard)
            {
                float h = UnityEngine.Input.GetAxisRaw("Horizontal");
                float v = UnityEngine.Input.GetAxisRaw("Vertical");
                Vector2 kb = new Vector2(h, v);
                if (kb.sqrMagnitude > 1f) kb.Normalize();
                return kb;
            }

            return Vector2.zero;
        }

        private void ReadModifiers()
        {
            bool sneakFromKeyboard = enableKeyboard && UnityEngine.Input.GetKey(keyboardSneak);
            SneakHeld = _sneakFromUI || sneakFromKeyboard;

            bool sprintFromKeyboard = enableKeyboard && UnityEngine.Input.GetKey(keyboardSprint);
            SprintHeld = _sprintFromUI || sprintFromKeyboard;
        }
    }
}