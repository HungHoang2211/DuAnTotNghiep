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

        // Sneak – PC: giữ Ctrl (hold) | Mobile: nhấn button để toggle
        private bool _sneakToggled;    

        public Vector2 MovementAxis
        {
            get
            {
                if (_mobileJoystick != null && _mobileJoystick.IsPressed)
                    return _mobileJoystick.Axis;

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
                if (_mobileJoystick != null && _mobileJoystick.IsPressed)
                    return _mobileJoystick.SprintHeld;

                return Input.GetKey(KeyCode.LeftControl)
                    || Input.GetKey(KeyCode.RightControl);
            }
        }
        public bool SneakHeld
        {
            get
            {
                // Mobile toggle button
                if (_sneakToggled) return true;

                // PC hold Ctrl
                return Input.GetKey(KeyCode.LeftControl)
                    || Input.GetKey(KeyCode.RightControl);
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

        public void OnAttackButtonUp() => _attackHeld = false;

        /// Gắn vào OnClick của Button Sneak trên Canvas.
        /// Nhấn lần 1 → sneak, nhấn lần 2 → đứng.
        /// TopDownMover vẫn có thể override nếu có trần thấp.
        public void OnSneakButtonClick() => _sneakToggled = !_sneakToggled;

        ///Cho phép TopDownMover ép tắt toggle (ví dụ khi bị block đứng dậy).</summary>
        public void ForceSneak(bool value) => _sneakToggled = value;


        private void LateUpdate()
        {
            _attackPressedThisFrame = false;
        }
    }
}