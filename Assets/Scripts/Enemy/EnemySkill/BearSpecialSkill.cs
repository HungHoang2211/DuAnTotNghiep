using UnityEngine;
using SimpleSurvival.Combat;

namespace SimpleSurvival.AI
{
    public sealed class BearSpecialSkill : BaseEnemySkill
    {
        [Header("Damage")]
        [SerializeField] private float damage = 35f;
        [SerializeField] private float damageRangeBonus = 0.5f;

        [Header("Refs")]
        [SerializeField] private BearAnimator animator;
        [SerializeField] private BaseEnemyController controller;

        private Transform _target;

        protected override void OnExecute(Transform target)
        {
            _target = target;
            if (animator != null) animator.TriggerSpecialAttack();
        }

        public void OnSpecialAttackHit()
        {
            if (!_isExecuting || _target == null) return;

            float dist = Vector3.Distance(transform.position, _target.position);
            if (dist > maxRange + damageRangeBonus) return;

            var damageable = ResolveDamageable(_target);
            if (damageable == null || damageable.IsDead) return;

            damageable.TakeDamage(damage, gameObject);
        }

        public void OnSpecialAttackEnd()
        {
            MarkComplete();
            if (controller != null) controller.NotifySkillComplete();
        }

        protected override void OnCancel()
        {
            if (animator != null) animator.CancelSpecialAttack();
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