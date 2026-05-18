using UnityEngine;

namespace Xyla.Player
{
    public class MovementAimer : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private float _rotationSpeedDegPerSec = 720f;

        [Tooltip("Camera dùng để tính hướng relative. Để trống = Camera.main.")]
        [SerializeField] private Camera _camera;

        private void Awake()
        {
            if (_camera == null) _camera = Camera.main;
        }

        private void Update()
        {
            Vector2 axis = _input.MovementAxis;
            if (axis.sqrMagnitude < 0.01f) return;

            float camYaw = _camera.transform.eulerAngles.y;
            Quaternion flatRot = Quaternion.Euler(0f, camYaw, 0f);
            Vector3 forward = flatRot * Vector3.forward;
            Vector3 right = flatRot * Vector3.right;

            Vector3 moveDir = (forward * axis.y + right * axis.x).normalized;
            if (moveDir.sqrMagnitude < 0.001f) return;

            Quaternion target = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                target,
                _rotationSpeedDegPerSec * Time.deltaTime);
        }
    }
}