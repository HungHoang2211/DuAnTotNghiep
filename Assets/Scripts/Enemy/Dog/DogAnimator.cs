using System;
using UnityEngine;

namespace SimpleSurvival.Pets
{
    public sealed class DogAnimator : MonoBehaviour
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int MoveModeHash = Animator.StringToHash("MoveMode");
        private static readonly int AttackIndexHash = Animator.StringToHash("AttackIndex");
        private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
        private static readonly int IsLyingHash = Animator.StringToHash("IsLying");
        private static readonly int StandUpHash = Animator.StringToHash("StandUp");

        [SerializeField] private Animator _animator;
        [SerializeField] private int moveModeNormal = 0;
        [SerializeField] private int moveModeSneak = 1;
        [SerializeField] private float speedDampTime = 0.1f;

        public event Action OnStandUpFinished;

        private void Awake()
        {
            if (_animator == null) _animator = GetComponent<Animator>();
        }

        public void SetSpeed(float normalizedSpeed)
        {
            _animator.SetFloat(SpeedHash, normalizedSpeed, speedDampTime, Time.deltaTime);
        }

        public void SetIdle()
        {
            _animator.SetFloat(SpeedHash, 0f, speedDampTime, Time.deltaTime);
        }

        public void SetSneaking(bool sneaking)
        {
            _animator.SetInteger(MoveModeHash, sneaking ? moveModeSneak : moveModeNormal);
        }

        public void SetLying(bool lying)
        {
            _animator.SetBool(IsLyingHash, lying);
        }

        public void TriggerStandUp()
        {
            _animator.SetBool(IsLyingHash, false);
            _animator.SetTrigger(StandUpHash);
        }

        public void NotifyStandUpFinished()
        {
            OnStandUpFinished?.Invoke();
        }

        public void TriggerAttack(int attackIndex)
        {
            _animator.SetInteger(AttackIndexHash, attackIndex);
            _animator.SetTrigger(AttackTriggerHash);
        }

        public void CancelAttack()
        {
            _animator.ResetTrigger(AttackTriggerHash);
        }
    }
}