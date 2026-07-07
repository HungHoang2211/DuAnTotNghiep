using UnityEngine;

namespace SimpleSurvival.AI
{
    public sealed class ZombieWitchAnimator : BaseEnemyAnimator
    {
        private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
        private static readonly int AttackLeftHash = Animator.StringToHash("AttackLeft");
        private static readonly int AttackRightHash = Animator.StringToHash("AttackRight");
        private static readonly int AttackBothHash = Animator.StringToHash("AttackBoth");
        private static readonly int HowlHash = Animator.StringToHash("Howl");
        private static readonly int IsDeadHash = Animator.StringToHash("IsDead");
        private static readonly int HasDroppedArmHash = Animator.StringToHash("HasDroppedArm");

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

        public void SetHasDroppedArm(bool value)
        {
            _animator.SetBool(HasDroppedArmHash, value);
        }

        public override void TriggerAttack(int attackIndex)
        {
            switch (attackIndex)
            {
                case 0: TriggerAttackLeft(); break;
                case 1: TriggerAttackRight(); break;
                default: TriggerAttackBoth(); break;
            }
        }

        public void TriggerAttackLeft() => _animator.SetTrigger(AttackLeftHash);
        public void TriggerAttackRight() => _animator.SetTrigger(AttackRightHash);
        public void TriggerAttackBoth() => _animator.SetTrigger(AttackBothHash);

        public override void TriggerHowl()
        {
            _animator.SetTrigger(HowlHash);
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
            _animator.SetBool(HasDroppedArmHash, false);
            _animator.SetFloat(MoveSpeedHash, 0f);
            _animator.Rebind();
            _animator.Update(0f);
        }

        public override void CancelAttack()
        {
            _animator.ResetTrigger(AttackLeftHash);
            _animator.ResetTrigger(AttackRightHash);
            _animator.ResetTrigger(AttackBothHash);
        }

        // Animation Event đặt trên clip Howl, tại frame minion xuất hiện
        public void AnimEvent_HowlSpawn() => RaiseHowlSpawn();

        // Animation Event đặt ở frame cuối clip Howl
        public void AnimEvent_HowlFinished() => RaiseHowlFinished();
    }
}