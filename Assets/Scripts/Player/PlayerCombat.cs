using UnityEngine;
using Xyla.Combat;

namespace Xyla.Player
{
    public class PlayerCombat : MonoBehaviour
    {
        private static readonly int AttackParam = Animator.StringToHash("Attack");

        [Header("Input")]
        [SerializeField] private PlayerInputReader _input;

        [Header("Combat")]
        [Tooltip("Damage mỗi lần đánh.")]
        [SerializeField] private float _damage = 25f;

        [Tooltip("Tầm đánh (bán kính OverlapSphere). Tăng theo độ dài vũ khí.")]
        [SerializeField] private float _attackRange = 1.5f;

        [Tooltip("Góc hình quạt phía trước nhân vật để tránh đánh ra sau lưng (độ).")]
        [SerializeField][Range(60f, 360f)] private float _attackAngle = 180f;

        [Tooltip("Cooldown giữa 2 lần đánh (giây). Set xấp xỉ độ dài animation.")]
        [SerializeField] private float _attackCooldown = 0.4f;

        [Tooltip("Layer của Enemy. Dùng để OverlapSphere chỉ detect enemy.")]
        [SerializeField] private LayerMask _enemyLayer;

        [Header("Animation (optional)")]
        [SerializeField] private Animator _animator;

        [Header("Audio (optional)")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _attackClip;
        [SerializeField][Range(0f, 1f)] private float _attackVolume = 1f;

        private float _nextAllowedAttackTime;
        private readonly Collider[] _hitBuffer = new Collider[16];

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
            DetectAndDamageEnemies();
        }

        private void DetectAndDamageEnemies()
        {
            Vector3 origin = transform.position + transform.forward * (_attackRange * 0.3f);

            int hitCount = Physics.OverlapSphereNonAlloc(
                origin, _attackRange, _hitBuffer, _enemyLayer);

            for (int i = 0; i < hitCount; i++)
            {
                if (!IsInAttackAngle(_hitBuffer[i].transform.position)) continue;

                // Tìm IDamageable trên enemy hoặc parent của nó
                var damageable = _hitBuffer[i].GetComponentInParent<IDamageable>();
                if (damageable == null || damageable.IsDead) continue;

                damageable.TakeDamage(_damage, gameObject);
                Debug.Log($"Hit {_hitBuffer[i].name} for {_damage} damage. " +
                          $"Remaining HP: {damageable.CurrentHealth}");
            }
        }

        private bool IsInAttackAngle(Vector3 targetPosition)
        {
            Vector3 toTarget = (targetPosition - transform.position).normalized;
            toTarget.y = 0f;
            float angle = Vector3.Angle(transform.forward, toTarget);
            return angle <= _attackAngle * 0.5f;
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

        // Vẽ tầm đánh trong Scene view để dễ chỉnh
        private void OnDrawGizmosSelected()
        {
            Vector3 origin = transform.position + transform.forward * (_attackRange * 0.3f);

            // Vòng tròn tầm đánh
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.3f);
            Gizmos.DrawWireSphere(origin, _attackRange);

            // Góc hình quạt
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
            float halfAngle = _attackAngle * 0.5f;
            Vector3 leftDir = Quaternion.Euler(0, -halfAngle, 0) * transform.forward;
            Vector3 rightDir = Quaternion.Euler(0, halfAngle, 0) * transform.forward;
            Gizmos.DrawRay(transform.position, leftDir * _attackRange);
            Gizmos.DrawRay(transform.position, rightDir * _attackRange);
        }
    }
}