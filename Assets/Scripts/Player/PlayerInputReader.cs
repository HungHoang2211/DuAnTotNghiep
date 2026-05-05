using UnityEngine;

namespace Xyla.Player
{
    /// <summary>
    /// Bọc UnityEngine.Input (legacy) sau các property tường minh.
    /// Các component gameplay phụ thuộc vào lớp này thay vì Input trực tiếp,
    /// nhờ vậy có thể swap nguồn input (AI, replay, network) mà không sửa code gameplay.
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        public Vector2 MovementAxis => new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical"));

        public Vector3 MouseScreenPosition => Input.mousePosition;

        public bool SprintHeld => Input.GetKey(KeyCode.LeftShift)
                               || Input.GetKey(KeyCode.RightShift);

        public bool AttackPressed => Input.GetMouseButtonDown(0);

        public bool AttackHeld => Input.GetMouseButton(0);

        public bool AimHeld => Input.GetMouseButton(1);

        public bool InteractPressed => Input.GetKeyDown(KeyCode.E);

        public bool BuildModeToggled => Input.GetKeyDown(KeyCode.B);
    }
}
