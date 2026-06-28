using UnityEngine;
using SimpleSurvival.Combat;

namespace SimpleSurvival.AI
{
    public sealed class BossClawComboSkill : BaseEnemySkill
    {
        [Header("Damage")]
        [SerializeField] private float damagePerHit = 25f;
        [SerializeField] private float damageRangeBonus = 0.5f;

        [Header("Refs")]
        [SerializeField] private ZombieBossAnimator animator;
        [SerializeField] private BaseEnemyController controller;

        private Transform _target;

        protected override void OnExecute(Transform target)
        {
            _target = target;
            if (animator != null) animator.TriggerAttackClaw();
        }

        // Animation Event ở frame trái trúng
        public void OnHitLeft() => TryDealDamage();

        // Animation Event ở frame phải trúng
        public void OnHitRight() => TryDealDamage();

        // Animation Event cuối combo
        public void OnAttackEnd()
        {
            MarkComplete();
            if (controller != null) controller.NotifySkillComplete();
        }

        private void TryDealDamage()
        {
            if (!_isExecuting || _target == null) return;

            float dist = Vector3.Distance(transform.position, _target.position);
            if (dist > maxRange + damageRangeBonus) return;

            var damageable = ResolveDamageable(_target);
            if (damageable == null || damageable.IsDead) return;

            damageable.TakeDamage(damagePerHit, gameObject);
        }

        protected override void OnCancel()
        {
            _target = null;
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