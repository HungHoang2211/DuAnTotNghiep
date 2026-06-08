using UnityEngine;

namespace Xyla.Player
{
    public class MouseAimer : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private float _rotationSpeedDegPerSec = 720f;
        [SerializeField] private float _aimPlaneYOffset = 0f;

        private Camera _camera;

        private void Awake()
        {
            _camera = Camera.main;
        }

        private void Update()
        {
            if (!TryGetAimPointOnGround(out Vector3 aimPoint)) return;

            Vector3 lookDirection = FlattenToHorizontal(aimPoint - transform.position);
            if (IsTooSmallToAim(lookDirection)) return;

            RotateToward(lookDirection);
        }

        private bool TryGetAimPointOnGround(out Vector3 aimPoint)
        {
            float planeY = transform.position.y + _aimPlaneYOffset;
            Plane aimPlane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));
            Ray ray = _camera.ScreenPointToRay(_input.MouseScreenPosition);

            if (aimPlane.Raycast(ray, out float distance))
            {
                aimPoint = ray.GetPoint(distance);
                return true;
            }

            aimPoint = default;
            return false;
        }

        private static Vector3 FlattenToHorizontal(Vector3 direction)
        {
            direction.y = 0f;
            return direction;
        }

        private static bool IsTooSmallToAim(Vector3 direction)
        {
            const float minSqrMagnitude = 0.0001f;
            return direction.sqrMagnitude < minSqrMagnitude;
        }

        private void RotateToward(Vector3 lookDirection)
        {
            Quaternion target = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                target,
                _rotationSpeedDegPerSec * Time.deltaTime);
        }
    }
}