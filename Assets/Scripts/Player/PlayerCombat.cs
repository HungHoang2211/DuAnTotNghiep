using UnityEngine;

namespace Xyla.Player
{
    /// <summary>
    /// Phát hiện input attack → kích trigger animation + phát attack sound.
    /// Có cooldown để không spam.
    ///
    /// Class này KHÔNG khoá movement → vừa di chuyển vừa attack được.
    /// Để attack animation không che mất chân chạy, setup Animator Controller
    /// với Layer 1 = upper-body Avatar Mask, attack state nằm ở layer này.
    /// (Locomotion blend tree ở Layer 0, weight = 1, vẫn drive chân.)
    ///
    /// Animator và AudioSource là TÙY CHỌN: chưa import animation/sound thì để trống,
    /// attack vẫn fire (chỉ là không thấy/không nghe).
    /// </summary>
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
