using UnityEngine;
using SimpleSurvival.Items;

namespace SimpleSurvival.Player
{
    public class PlayerToolSwapper : MonoBehaviour
    {
        [SerializeField] private Animator animator;

        private RuntimeAnimatorController _savedController;

        public bool IsSwapped { get; private set; }
        public ToolAbility CurrentTool { get; private set; }

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        public bool SwapIn(ToolAbility tool)
        {
            if (tool == null || tool.OverrideController == null) return false;
            if (animator == null) return false;
            if (IsSwapped) SwapOut();

            _savedController = animator.runtimeAnimatorController;
            animator.runtimeAnimatorController = tool.OverrideController;
            CurrentTool = tool;
            IsSwapped = true;
            return true;
        }

        public void SwapOut()
        {
            if (!IsSwapped) return;
            if (animator != null && _savedController != null)
                animator.runtimeAnimatorController = _savedController;

            _savedController = null;
            CurrentTool = null;
            IsSwapped = false;
        }
    }
}