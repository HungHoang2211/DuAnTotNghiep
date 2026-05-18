using UnityEngine;

namespace Xyla.Player
{
    public class PlayerCombat : MonoBehaviour
    {
        private static readonly int AttackParam = Animator.StringToHash("Attack");

        [Header("Input")]
        [SerializeField] private PlayerInputReader _input;

        [Header("Combat")]
        [Tooltip("Khoảng thời gian tối thiểu giữa 2 lần attack (giây). " +
                 "Set xấp xỉ độ dài animation attack để không spam.")]
        [SerializeField] private float _attackCooldown = 0.4f;

        [Header("Animation (optional)")]
        [SerializeField] private Animator _animator;

        [Header("Audio (optional)")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _attackClip;
        [SerializeField][Range(0f, 1f)] private float _attackVolume = 1f;

        private float _nextAllowedAttackTime;

        private void Update()
        {
            if (!_input.AttackPressed) return;
            if (Time.time < _nextAllowedAttackTime) return;

            _nextAllowedAttackTime = Time.time + _attackCooldown;
            PerformAttack();
        }

        private void PerformAttack()
        {
            TriggerAttackAnimation();
            PlayAttackSound();
            Debug.Log("Attack");
        }

        private void TriggerAttackAnimation()
        {
            if (_animator == null) return;
            _animator.SetTrigger(AttackParam);
        }

        private void PlayAttackSound()
        {
            if (_audioSource == null || _attackClip == null) return;
            _audioSource.PlayOneShot(_attackClip, _attackVolume);
        }
    }
}
