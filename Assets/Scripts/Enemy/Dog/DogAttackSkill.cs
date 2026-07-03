using UnityEngine;
using SimpleSurvival.Combat;

namespace SimpleSurvival.Pets
{
    public sealed class DogAttackSkill : MonoBehaviour
    {
        [Header("Damage")]
        [SerializeField] private float damage = 10f;
        [SerializeField] private float range = 1.5f;
        [SerializeField] private float damageRangeBonus = 0.5f;
        [SerializeField] private float cooldown = 2f;
        [SerializeField] private int attackAnimCount = 2;

        [Header("Refs")]
        [SerializeField] private DogAnimator animator;
        [SerializeField] private DogController controller;

        private float _lastExecuteTime = -999f;
        private bool _isExecuting;
        private Transform _target;

        public bool IsExecuting => _isExecuting;
        public float Range => range;

        public bool IsAvailable(Transform target, float distanceToTarget)
        {
            if (_isExecuting) return false;
            if (target == null) return false;
            if (Time.time < _lastExecuteTime + cooldown) return false;
            if (distanceToTarget > range) return false;
            return true;
        }

        public void Execute(Transform target)
        {
            if (!IsAvailable(target, Vector3.Distance(transform.position, target.position)))
                return;

            _target = target;
            _lastExecuteTime = Time.time;
            _isExecuting = true;

            int attackIndex = Random.Range(0, attackAnimCount);
            if (animator != null) animator.TriggerAttack(attackIndex);
        }

        public void Cancel()
        {
            _isExecuting = false;
            _target = null;
            if (animator != null) animator.CancelAttack();
        }

        /// <summary>
        /// Ép skill vào cooldown ngay từ thời điểm gọi, dùng để trì hoãn
        /// đòn tấn công đầu tiên khi Dog vừa vào combat (giữ tư thế idle tấn công trước).
        /// </summary>
        public void PutOnCooldown()
        {
            _lastExecuteTime = Time.time;
        }

        // Animation Event: gắn vào frame ra đòn trong clip attack
        public void OnAttackHit()
        {
            if (!_isExecuting || _target == null) return;

            float dist = Vector3.Distance(transform.position, _target.position);
            if (dist > range + damageRangeBonus) return;

            IDamageable damageable = ResolveDamageable(_target);
            if (damageable == null || damageable.IsDead) return;

            damageable.TakeDamage(damage, gameObject);
        }

        // Animation Event: gắn vào frame cuối clip attack
        public void OnAttackEnd()
        {
            _isExecuting = false;
            _target = null;
            if (controller != null) controller.NotifySkillComplete();
        }

        private IDamageable ResolveDamageable(Transform target)
        {
            var direct = target.GetComponent<IDamageable>();
            if (direct != null) return direct;

            var inChildren = target.GetComponentInChildren<IDamageable>();
            if (inChildren != null) return inChildren;

            return target.GetComponentInParent<IDamageable>();
        }
    }
}