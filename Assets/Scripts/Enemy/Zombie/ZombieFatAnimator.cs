using UnityEngine;

namespace SimpleSurvival.AI
{
    public sealed class ZombieFatAnimator : BaseEnemyAnimator
    {
        private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
        private static readonly int AttackClawHash = Animator.StringToHash("AttackClaw");
        private static readonly int ClawIndexHash = Animator.StringToHash("ClawIndex");
        private static readonly int JumpAttackHash = Animator.StringToHash("JumpAttack");
        private static readonly int IsDeadHash = Animator.StringToHash("IsDead");

        public bool IsInAttackState
        {
            get
            {
                if (_animator == null) return false;
                var info = _animator.GetCurrentAnimatorStateInfo(0);
                return info.IsTag("Attack");
            }
        }

        public override void SetMoving(bool moving)
        {
            _animator.SetFloat(MoveSpeedHash, moving ? 1f : 0f);
        }

        public void SetMoveSpeed(float speed)
        {
            _animator.SetFloat(MoveSpeedHash, speed);
        }

        public override void SetIdle()
        {
            _animator.SetFloat(MoveSpeedHash, 0f);
        }

        public override void TriggerAttack(int attackIndex)
        {
            TriggerAttackClaw();
        }

        public void TriggerAttackClaw()
        {
            _animator.SetInteger(ClawIndexHash, Random.Range(0, 2));
            _animator.SetTrigger(AttackClawHash);
        }

        public void TriggerJumpAttack()
        {
            _animator.SetTrigger(JumpAttackHash);
        }

        public override void TriggerDeath()
        {
            _animator.SetBool(IsDeadHash, true);
            _animator.enabled = false;
            SetRagdollActive(true);
        }

        public override void ResetForSpawn()
        {
            SetRagdollActive(false);
            _animator.enabled = true;
            _animator.SetBool(IsDeadHash, false);
            _animator.SetFloat(MoveSpeedHash, 0f);
            _animator.SetInteger(ClawIndexHash, 0);
            _animator.Rebind();
            _animator.Update(0f);
        }

        public override void CancelAttack()
        {
            _animator.ResetTrigger(AttackClawHash);
            _animator.ResetTrigger(JumpAttackHash);
        }
    }
}