using UnityEngine;

namespace SimpleSurvival.AI
{
    public sealed class ZombieAnimator : BaseEnemyAnimator
    {
        private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
        private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
        private static readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");
        private static readonly int IsHowlingHash = Animator.StringToHash("IsHowling");
        private static readonly int IsDeadHash = Animator.StringToHash("IsDead");

        public override void SetMoving(bool moving)
        {
            _animator.SetBool(IsWalkingHash, moving);
        }

        public void SetLocomotion(bool moving, bool running)
        {
            if (running)
            {
                _animator.SetBool(IsRunningHash, moving);
                _animator.SetBool(IsWalkingHash, false);
            }
            else
            {
                _animator.SetBool(IsWalkingHash, moving);
                _animator.SetBool(IsRunningHash, false);
            }
        }

        public override void SetIdle()
        {
            _animator.SetBool(IsWalkingHash, false);
            _animator.SetBool(IsRunningHash, false);
        }

        public override void TriggerAttack(int attackIndex)
        {
            _animator.SetTrigger(IsAttackingHash);
        }

        public void SetHowling(bool active)
        {
            _animator.SetBool(IsHowlingHash, active);
        }

        public override void TriggerDeath()
        {
            _animator.enabled = false;
            SetRagdollActive(true);
        }

        public override void ResetForSpawn()
        {
            SetRagdollActive(false);
            _animator.enabled = true;
            _animator.SetBool(IsDeadHash, false);
            _animator.SetBool(IsHowlingHash, false);
            _animator.SetBool(IsWalkingHash, false);
            _animator.SetBool(IsRunningHash, false);
            _animator.Rebind();
            _animator.Update(0f);
        }

        public override void CancelAttack()
        {
            _animator.ResetTrigger(IsAttackingHash);
        }
    }
}