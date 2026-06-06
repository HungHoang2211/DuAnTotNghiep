using SimpleSurvival.Cameras;
using UnityEngine;

namespace SimpleSurvival.Input
{
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

        public Vector3 WorldDirection { get; private set; } = Vector3.zero;
        public float Magnitude { get; private set; } = 0f;

        public bool HasInput => Magnitude > 0.01f;

        public bool SneakHeld { get; private set; }

        public bool SprintHeld { get; private set; }

        private bool _sneakFromUI = false;
        private bool _sprintFromUI = false;
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
                cameraRig = FindFirstObjectByType<CameraRigController>();
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