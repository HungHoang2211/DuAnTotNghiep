using UnityEngine;
using Xyla.Combat;
using Xyla.Enemy;

namespace Xyla.Player
{
    public class PlayerCombat : MonoBehaviour
    {
        private static readonly int AnimAttack = Animator.StringToHash("Attack");

        [Header("Input")]
        [SerializeField] private PlayerInputReader _input;

        [Header("Combat")]
        [SerializeField] private float _damage = 25f;
        [SerializeField] private float _attackRange = 1.5f;
        [SerializeField][Range(60f, 360f)] private float _attackAngle = 180f;
        [SerializeField] private float _attackCooldown = 0.6f;
        [SerializeField] private LayerMask _enemyLayer;

        [Header("Animation (optional)")]
        [SerializeField] private Animator _animator;

        [Header("Audio (optional)")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _attackClip;
        [SerializeField][Range(0f, 1f)] private float _attackVolume = 1f;

        private float _nextAttackTime;
        private readonly Collider[] _hitBuffer = new Collider[16];

        private void Update()
        {
            if (!_input.AttackPressed) return;
            if (Time.time < _nextAttackTime) return;

            _nextAttackTime = Time.time + _attackCooldown;
            PerformAttack();
        }

        private void PerformAttack()
        {
            if (_animator != null) _animator.SetTrigger(AnimAttack);
            if (_audioSource != null && _attackClip != null)
                _audioSource.PlayOneShot(_attackClip, _attackVolume);

            Vector3 origin = transform.position + transform.forward * (_attackRange * 0.3f);
            int hitCount = Physics.OverlapSphereNonAlloc(origin, _attackRange, _hitBuffer, _enemyLayer);

            for (int i = 0; i < hitCount; i++)
            {
                Vector3 toTarget = (_hitBuffer[i].transform.position - transform.position);
                toTarget.y = 0f;
                if (Vector3.Angle(transform.forward, toTarget.normalized) > _attackAngle * 0.5f) continue;

                var dmg = _hitBuffer[i].GetComponentInParent<IDamageable>();
                if (dmg == null || dmg.IsDead) continue;

                dmg.TakeDamage(_damage, gameObject);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = transform.position + transform.forward * (_attackRange * 0.3f);
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.3f);
            Gizmos.DrawWireSphere(origin, _attackRange);
            float h = _attackAngle * 0.5f;
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
            Gizmos.DrawRay(transform.position, Quaternion.Euler(0, -h, 0) * transform.forward * _attackRange);
            Gizmos.DrawRay(transform.position, Quaternion.Euler(0, h, 0) * transform.forward * _attackRange);
        }
    }
}