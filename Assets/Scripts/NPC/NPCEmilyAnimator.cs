using UnityEngine;

namespace SimpleSurvival.AI
{
    [RequireComponent(typeof(Animator))]
    public sealed class NPCEmilyAnimator : MonoBehaviour
    {
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
        private static readonly int AttackIndexHash = Animator.StringToHash("AttackIndex");
        private static readonly int DeathTriggerHash = Animator.StringToHash("Death");

        [SerializeField] private Animator animator;

        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
        }

        public void SetMoving(bool moving)
        {
            if (animator != null) animator.SetBool(IsMovingHash, moving);
        }
        public void TriggerRandomAttack()
        {
            if (animator == null) return;
            int index = Random.Range(0, 3);
            animator.SetInteger(AttackIndexHash, index);
            animator.SetTrigger(AttackTriggerHash);
        }

        public void TriggerDeath()
        {
            if (animator != null) animator.SetTrigger(DeathTriggerHash);
        }
    }
}