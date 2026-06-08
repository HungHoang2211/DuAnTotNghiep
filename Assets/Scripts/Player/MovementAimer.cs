using UnityEngine;

namespace Xyla.Player
{
    public class MovementAimer : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private float _rotationSpeedDegPerSec = 720f;

        private void Update()
        {
            Vector2 axis = _input.MovementAxis;
            if (axis.sqrMagnitude < 0.01f) return;

            // Joystick lên = +Z, phải = +X (world space)
            // Camera Y=45° chỉ ảnh hưởng visual, không ảnh hưởng world movement
            Vector3 moveDir = new Vector3(axis.x, 0f, axis.y).normalized;
            if (moveDir.sqrMagnitude < 0.001f) return;

            Quaternion target = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, target,
                _rotationSpeedDegPerSec * Time.deltaTime);
        }
    }
}