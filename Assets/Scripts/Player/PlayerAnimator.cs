using UnityEngine;

namespace SimpleSurvival.Player
{
    [RequireComponent(typeof(PlayerMovement))]
    public class PlayerAnimator : MonoBehaviour
    {
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
            animator.SetFloat(ParamMoveSpeed, _movement.NormalizedSpeed, speedDampTime, Time.deltaTime);
        }

        private void UpdateMoveMode()
        {
            int mode = _movement.IsSneaking ? moveModeSneak : moveModeNormal;
            animator.SetInteger(ParamMoveMode, mode);
        }
    }
}