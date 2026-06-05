using UnityEngine;
using SimpleSurvival.Input;

namespace SimpleSurvival.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement")]
        [Tooltip("Tốc độ di chuyển tối đa (đơn vị/giây).")]
        [SerializeField] private float moveSpeed = 5f;

        [Tooltip("Độ mượt khi xoay nhân vật theo direction. Cao = quay nhanh.")]
        [SerializeField, Range(1f, 30f)] private float rotationSmoothness = 12f;

        [Header("Gravity")]
        [SerializeField] private float gravity = -20f;

        private CharacterController _controller;
        private PlayerInput _input;

        private float _verticalVelocity = 0f;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<PlayerInput>();
        }

        private void Update()
        {
            HandleMovement();
            HandleRotation();
        }

        private void HandleMovement()
        {
            Vector3 horizontalVelocity = _input.WorldDirection * (moveSpeed * _input.Magnitude);

            if (_controller.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;
            else
                _verticalVelocity += gravity * Time.deltaTime;
            Vector3 velocity = horizontalVelocity + Vector3.up * _verticalVelocity;
            _controller.Move(velocity * Time.deltaTime);
        }

        private void HandleRotation()
        {
            if (!_input.HasInput) return;

            Quaternion targetRot = Quaternion.LookRotation(_input.WorldDirection, Vector3.up);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                rotationSmoothness * Time.deltaTime
            );
        }
    }
}