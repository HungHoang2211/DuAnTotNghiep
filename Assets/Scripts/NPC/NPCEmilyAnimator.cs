using System;
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

        public event Action OnAttackHit;

        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
        }

        public void SetMoving(bool moving)
        {
            if (animator != null) animator.SetBool(IsMovingHash, moving);
        }

        private static readonly float[] AttackIndexThresholds = { 0f, 0.5f, 1f };

        public void TriggerRandomAttack()
        {
            if (animator == null) return;
            float value = AttackIndexThresholds[UnityEngine.Random.Range(0, AttackIndexThresholds.Length)];
            animator.SetFloat(AttackIndexHash, value);
            animator.SetTrigger(AttackTriggerHash);
        }

        public void TriggerDeath()
        {
            if (animator != null) animator.SetTrigger(DeathTriggerHash);
        }

        public void AnimEvent_AttackHit()
        {
            OnAttackHit?.Invoke();
        }
    }
}