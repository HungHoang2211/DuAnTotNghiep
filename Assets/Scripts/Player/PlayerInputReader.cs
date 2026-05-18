using UnityEngine;

namespace Xyla.Player
{
    public class PlayerInputReader : MonoBehaviour
    {
        [Header("Mobile Input (để trống nếu chỉ build PC)")]
        [Tooltip("Kéo component MobileJoystick vào đây để bật mobile input.")]
        [SerializeField] private MobileJoystick _mobileJoystick;

        private bool _attackPressedThisFrame;
        private bool _attackHeld;

        public Vector2 MovementAxis
        {
            get
            {
                if (_mobileJoystick != null && _mobileJoystick.IsPressed)
                    return _mobileJoystick.Axis;

                // Fallback: keyboard
                return new Vector2(
                    Input.GetAxisRaw("Horizontal"),
                    Input.GetAxisRaw("Vertical"));
            }
        }

        public Vector3 MouseScreenPosition => Input.mousePosition;

        public bool SprintHeld
        {
            get
            {
                // Mobile: joystick kéo mạnh = sprint
                if (_mobileJoystick != null && _mobileJoystick.IsPressed)
                    return _mobileJoystick.SprintHeld;

                // PC: Shift
                return Input.GetKey(KeyCode.LeftShift)
                    || Input.GetKey(KeyCode.RightShift);
            }
        }

        public bool AttackPressed => Input.GetMouseButtonDown(0) || _attackPressedThisFrame;
        public bool AttackHeld => Input.GetMouseButton(0) || _attackHeld;
        public bool AimHeld => Input.GetMouseButton(1);
        public bool InteractPressed => Input.GetKeyDown(KeyCode.E);
        public bool BuildModeToggled => Input.GetKeyDown(KeyCode.B);
        public void OnAttackButtonDown()
        {
            _attackPressedThisFrame = true;
            _attackHeld = true;
        }

        public void OnAttackButtonUp()
        {
            _attackHeld = false;
        }


        private void LateUpdate()
        {
            _attackPressedThisFrame = false;
        }
    }
}