using UnityEngine;
using SimpleSurvival.Actions;
using SimpleSurvival.Input;

namespace SimpleSurvival.Player
{
    [RequireComponent(typeof(PlayerActionController))]
    public class PlayerAnimator : MonoBehaviour
    {
        private static readonly int ParamMoveSpeed = Animator.StringToHash("MoveSpeed");
        private static readonly int ParamMoveMode = Animator.StringToHash("MoveMode");

        [SerializeField] private Animator animator;
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private int moveModeNormal = 0;
        [SerializeField] private int moveModeSneak = 1;
        [SerializeField] private float speedDampTime = 0.1f;

        private PlayerActionController _actionController;

        private void Awake()
        {
            _actionController = GetComponent<PlayerActionController>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (inputReader == null) inputReader = GetComponent<PlayerInputReader>();
        }

        private void Update()
        {
            if (animator == null) return;

            float moveSpeed = 0f;

            if (_actionController.CurrentAction is MoveAction move)
                moveSpeed = move.NormalizedSpeed;

            bool isSneaking = inputReader != null && inputReader.IsSneakHeld;

            animator.SetFloat(ParamMoveSpeed, moveSpeed, speedDampTime, Time.deltaTime);
            animator.SetInteger(ParamMoveMode, isSneaking ? moveModeSneak : moveModeNormal);
        }
    }
}