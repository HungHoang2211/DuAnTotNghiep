using UnityEngine;
using Xyla.Combat;
using Xyla.Core;

namespace Xyla.Enemy
{
    /// <summary>
    /// Script máu cơ bản cho Enemy.
    /// Implement IDamageable để PlayerCombat có thể gọi TakeDamage().
    ///
    /// SETUP:
    ///   Gắn lên Enemy root, set Tag = "Enemy".
    ///   Kéo vào các field tùy chọn nếu có animation/audio/effect.
    /// </summary>
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        [Header("Stats")]
        [SerializeField] private float _maxHealth = 100f;

        [Header("On Hit (optional)")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _hitClip;
        [SerializeField] private AudioClip _deathClip;
        [SerializeField] private Animator _animator;

        // Animator params
        private static readonly int HitParam = Animator.StringToHash("Hit");
        private static readonly int DeathParam = Animator.StringToHash("Death");

        // IDamageable
        public float CurrentHealth { get; private set; }
        public bool IsDead { get; private set; }

        private void Awake()
        {
            CurrentHealth = _maxHealth;
        }

        // Gọi bởi ObjectPool khi enemy được lấy ra dùng lại
        private void OnSpawnFromPool()
        {
            CurrentHealth = _maxHealth;
            IsDead = false;
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = true;
        }

        public bool TakeDamage(float amount, GameObject source)
        {
            if (IsDead) return false;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);

            if (CurrentHealth <= 0f)
                Die();
            else
                OnHit();

            return !IsDead;
        }

        private void OnHit()
        {
            PlaySound(_hitClip);
            if (_animator != null) _animator.SetTrigger(HitParam);
        }

        private void Die()
        {
            IsDead = true;
            PlaySound(_deathClip);
            if (_animator != null) _animator.SetTrigger(DeathParam);

            // Tắt collider để không bị đánh tiếp
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            // Trả về pool thay vì Destroy — tái sử dụng, không tốn GC
            ObjectPool.Instance.ReturnDelayed(gameObject, 2f);
        }

        private void PlaySound(AudioClip clip)
        {
            if (_audioSource == null || clip == null) return;
            _audioSource.PlayOneShot(clip);
        }

        // Hiện thanh máu debug trong Scene view
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;
            float ratio = CurrentHealth / _maxHealth;
            Gizmos.color = Color.Lerp(Color.red, Color.green, ratio);
            Gizmos.DrawWireCube(
                transform.position + Vector3.up * 2f,
                new Vector3(ratio * 2f, 0.1f, 0.1f));
        }
    }
}