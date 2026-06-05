using UnityEngine;
using SimpleSurvival.Input;

namespace SimpleSurvival.Player
{
    /// <summary>
    /// Di chuyển player dựa trên input từ PlayerInput.
    /// Dùng Unity's CharacterController để có collision.
    /// 
    /// Trách nhiệm:
    /// - Di chuyển theo WorldDirection từ PlayerInput.
    /// - Xoay player root theo direction (lerp mượt).
    /// - Apply gravity đơn giản.
    /// 
    /// Gắn lên Player GameObject (cùng GameObject có CharacterController và PlayerInput).
    /// </summary>
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

        // Components
        private CharacterController _controller;
        private PlayerInput _input;

        // State
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
            // Horizontal movement từ input (đã bù camera angle ở PlayerInput)
            Vector3 horizontalVelocity = _input.WorldDirection * (moveSpeed * _input.Magnitude);

            // Gravity: tích lũy vận tốc rơi, reset khi chạm đất
            if (_controller.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;  // giá trị nhỏ âm giữ grounded ổn định
            else
                _verticalVelocity += gravity * Time.deltaTime;

            // Kết hợp horizontal + vertical
            Vector3 velocity = horizontalVelocity + Vector3.up * _verticalVelocity;
            _controller.Move(velocity * Time.deltaTime);
        }

        private void HandleRotation()
        {
            // Chỉ xoay khi có input (không xoay về facing 0 khi đứng yên)
            if (!_input.HasInput) return;

            // Target rotation: face theo direction di chuyển
            Quaternion targetRot = Quaternion.LookRotation(_input.WorldDirection, Vector3.up);

            // Lerp mượt thay vì snap rotation
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                rotationSmoothness * Time.deltaTime
            );
        }
    }
}