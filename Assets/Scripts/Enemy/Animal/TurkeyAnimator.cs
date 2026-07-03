using UnityEngine;

namespace SimpleSurvival.AI
{
    public sealed class TurkeyAnimator : BaseEnemyAnimator
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsDeadHash = Animator.StringToHash("IsDead");
        private static readonly int IsEatingHash = Animator.StringToHash("IsEating");

        public override void SetMoving(bool moving)
        {
            _animator.SetFloat(SpeedHash, moving ? 1f : 0f);
        }

        public void SetSpeed(float speed)
        {
            _animator.SetFloat(SpeedHash, speed);
        }

        public override void SetIdle()
        {
            _animator.SetFloat(SpeedHash, 0f);
            _animator.SetBool(IsEatingHash, false);
        }

        public void SetEating(bool eating)
        {
            _animator.SetBool(IsEatingHash, eating);
        }

        public override void TriggerAttack(int attackIndex) { }

        public override void TriggerDeath()
        {
            _animator.SetBool(IsDeadHash, true);
        }

        public override void ResetForSpawn()
        {
            _animator.SetBool(IsDeadHash, false);
            _animator.SetBool(IsEatingHash, false);
            _animator.SetFloat(SpeedHash, 0f);
            _animator.Rebind();
            _animator.Update(0f);
        }
    }
}