using UnityEngine;

namespace SimpleSurvival.AI
{
    public sealed class WolfAnimator : BaseEnemyAnimator
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsDeadHash = Animator.StringToHash("IsDead");
        private static readonly int IsAttackHash = Animator.StringToHash("IsAttacking");
        private static readonly int IsHowlingHash = Animator.StringToHash("IsHowling");
        private static readonly int DeathIndexHash = Animator.StringToHash("DeathIndex");

        [Header("Death Variation")]
        [SerializeField] private int _deathClipCount = 2;

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
            _animator.SetTrigger(IsAttackHash);
        }

        public void SetHowling(bool active)
        {
            _animator.SetBool(IsHowlingHash, active);
        }

        public override void TriggerDeath()
        {
            float randomIndex = Random.Range(0, _deathClipCount);
            _animator.SetFloat(DeathIndexHash, randomIndex);
            _animator.SetBool(IsDeadHash, true);
        }

        public override void ResetForSpawn()
        {
            _animator.SetBool(IsDeadHash, false);
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