using UnityEngine;
using SimpleSurvival.Combat;
using SimpleSurvival.Stats;

namespace SimpleSurvival.AI
{
    public sealed class WitchClawSkill : BaseEnemySkill
    {
        private enum AttackMode { Left, Right, Both }

        [Header("Damage (fallback nếu không có ZombieWitchStatsConfig)")]
        [SerializeField] private float damagePerArm = 20f;
        [SerializeField] private float bothArmsMultiplier = 1.3f;
        [SerializeField] private float damageRangeBonus = 0.5f;

        [Header("Refs")]
        [SerializeField] private ZombieWitchAnimator animator;
        [SerializeField] private ZombieWitchController controller;
        [SerializeField] private EnemyStats stats;

        private Transform _target;
        private AttackMode _currentMode;

        private ZombieWitchStatsConfig WitchConfig =>
            stats != null ? stats.EnemyConfig as ZombieWitchStatsConfig : null;

        protected override void OnExecute(Transform target)
        {
            _target = target;
            _currentMode = PickMode();

            switch (_currentMode)
            {
                case AttackMode.Left: animator?.TriggerAttackLeft(); break;
                case AttackMode.Right: animator?.TriggerAttackRight(); break;
                case AttackMode.Both: animator?.TriggerAttackBoth(); break;
            }
        }

        private AttackMode PickMode()
        {
            bool hasDropped = controller != null && controller.HasDroppedArm;
            if (!hasDropped)
                return (AttackMode)Random.Range(0, 3);

            return controller.DroppedArmIndex == 0 ? AttackMode.Right : AttackMode.Left;
        }

        // Animation Event ở frame tay trái trúng
        public void OnHitLeft() => TryDealDamage(1);

        // Animation Event ở frame tay phải trúng
        public void OnHitRight() => TryDealDamage(1);

        // Animation Event ở frame combo cả 2 tay trúng
        public void OnHitBoth() => TryDealDamage(2);

        // Animation Event cuối đòn đánh
        public void OnAttackEnd()
        {
            MarkComplete();
            if (controller != null) controller.NotifySkillComplete();
        }

        private void TryDealDamage(int armCount)
        {
            if (!_isExecuting || _target == null) return;

            float dist = Vector3.Distance(transform.position, _target.position);
            if (dist > maxRange + damageRangeBonus) return;

            var damageable = ResolveDamageable(_target);
            if (damageable == null || damageable.IsDead) return;

            float perArm = WitchConfig != null ? WitchConfig.DamagePerArm : damagePerArm;
            float multiplier = WitchConfig != null ? WitchConfig.BothArmsMultiplier : bothArmsMultiplier;

            float damage = armCount == 2 ? perArm * multiplier * 2f : perArm;
            damageable.TakeDamage(damage, gameObject);
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