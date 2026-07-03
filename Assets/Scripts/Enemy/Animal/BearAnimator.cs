using UnityEngine;

namespace SimpleSurvival.AI
{
    public sealed class BearAnimator : BaseEnemyAnimator
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsAttackHash = Animator.StringToHash("IsAttacking");
        private static readonly int AttackIndexHash = Animator.StringToHash("AttackIndex");
        private static readonly int IsHowlingHash = Animator.StringToHash("IsHowling");
        private static readonly int IsAttackSpecialHash = Animator.StringToHash("IsAttackingSpecial");

        [Header("Attack Variation")]
        [SerializeField] private int _attackClipCount = 2;

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
            _animator.SetBool(IsHowlingHash, false);
        }

        public override void TriggerAttack(int attackIndex)
        {
            float randomIndex = Random.Range(0, _attackClipCount);
            _animator.SetFloat(AttackIndexHash, randomIndex);
            _animator.SetTrigger(IsAttackHash);
        }

        public void SetHowling(bool active)
        {
            _animator.SetBool(IsHowlingHash, active);
        }

        public void TriggerSpecialAttack()
        {
            _animator.SetTrigger(IsAttackSpecialHash);
        }

        public void CancelSpecialAttack()
        {
            _animator.ResetTrigger(IsAttackSpecialHash);
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
            _animator.SetBool(IsHowlingHash, false);
            _animator.SetFloat(SpeedHash, 0f);
            _animator.Rebind();
            _animator.Update(0f);
        }

        public override void CancelAttack()
        {
            _animator.ResetTrigger(IsAttackHash);
        }
    }
}