using SimpleSurvival.Cameras;
using SimpleSurvival.Player;
using UnityEngine;

namespace SimpleSurvival.Input
{
    [RequireComponent(typeof(PlayerActionController))]
    public class PlayerInputReader : MonoBehaviour
    {
        [Header("Input Sources")]
        [SerializeField] private VirtualJoystick joystick;
        [SerializeField] private CameraRigController cameraRig;

        [Header("PC Keyboard Fallback")]
        [SerializeField] private bool enableKeyboard = true;
        [SerializeField] private KeyCode keyboardSneak = KeyCode.LeftControl;

        private PlayerActionController _actionController;
        private bool _sneakFromUI;

        public void SetSneakFromUI(bool held) => _sneakFromUI = held;
        public void SetSprintFromUI(bool held) { }

        public void ForceSneak(bool value)
        {
            _sneakFromUI = value;
        }

        private void Awake()
        {
            _actionController = GetComponent<PlayerActionController>();
            if (cameraRig == null)
                cameraRig = FindAnyObjectByType<CameraRigController>();
        }

        private void Update()
        {
            Vector2 rawInput = ReadRawMovementInput();
            Vector3 worldDir = Vector3.zero;
            float magnitude = 0f;

            if (rawInput.sqrMagnitude > 0.001f)
            {
                float yaw = cameraRig != null ? cameraRig.YawAngle : 45f;
                Vector3 inputAsWorld = new Vector3(rawInput.x, 0f, rawInput.y);
                worldDir = Quaternion.Euler(0f, yaw, 0f) * inputAsWorld;
                magnitude = Mathf.Clamp01(rawInput.magnitude);

                if (worldDir.sqrMagnitude > 0.001f)
                    worldDir = worldDir.normalized;
            }

            bool sneakHeld = _sneakFromUI || (enableKeyboard && UnityEngine.Input.GetKey(keyboardSneak));

            _actionController.RequestMove(worldDir, magnitude, sneakHeld);
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
    }
}