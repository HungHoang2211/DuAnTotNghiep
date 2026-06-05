using UnityEngine;

namespace SimpleSurvival.Player
{
    /// <summary>
    /// Bridge giữa PlayerMovement state và Animator parameters.
    /// 
    /// Trách nhiệm: ĐỌC state từ PlayerMovement, SET parameters cho Animator.
    /// KHÔNG drive movement, KHÔNG xử lý input.
    /// 
    /// Tách riêng để: nếu sau này đổi animator (model khác, controller khác),
    /// chỉ sửa file này, không động đến movement logic.
    /// </summary>
    [RequireComponent(typeof(PlayerMovement))]
    public class PlayerAnimator : MonoBehaviour
    {
        // Cached parameter hashes — nhanh hơn dùng string mỗi frame
        private static readonly int ParamMoveSpeed = Animator.StringToHash("MoveSpeed");
        private static readonly int ParamMoveMode = Animator.StringToHash("MoveMode");

        [Header("References")]
        [Tooltip("Animator của model. Có thể nằm trên GameObject con.")]
        [SerializeField] private Animator animator;

        [Header("MoveMode Values")]
        [Tooltip("Giá trị MoveMode khi đi/chạy bình thường. LDOE dùng 0.")]
        [SerializeField] private int moveModeNormal = 0;

        [Tooltip("Giá trị MoveMode khi sneak. LDOE dùng 4.")]
        [SerializeField] private int moveModeSneak = 4;

        [Header("Smoothing")]
        [Tooltip("Damping time cho MoveSpeed parameter. Cao = lerp mượt hơn, trễ hơn.")]
        [SerializeField] private float speedDampTime = 0.1f;

        private PlayerMovement _movement;

        private void Awake()
        {
            _movement = GetComponent<PlayerMovement>();

            // Tự tìm Animator nếu chưa gán (trên con)
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            if (animator == null) return;

            UpdateMoveSpeed();
            UpdateMoveMode();
        }

        private void UpdateMoveSpeed()
        {
            // NormalizedSpeed 0..1 — Animator blend tree tự pick clip phù hợp
            // 0 = idle, 0.5 = walk, 1 = run (tùy blend tree setup)
            animator.SetFloat(ParamMoveSpeed, _movement.NormalizedSpeed, speedDampTime, Time.deltaTime);
        }

        private void UpdateMoveMode()
        {
            int mode = _movement.IsSneaking ? moveModeSneak : moveModeNormal;
            animator.SetInteger(ParamMoveMode, mode);
        }
    }
}