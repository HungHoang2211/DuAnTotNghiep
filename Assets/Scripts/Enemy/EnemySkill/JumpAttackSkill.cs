using System.Collections;
using UnityEngine;
using SimpleSurvival.Combat;

namespace SimpleSurvival.AI
{
    public sealed class JumpAttackSkill : BaseEnemySkill
    {
        [Header("Damage")]
        [SerializeField] private float damage = 30f;
        [SerializeField] private float aoeRadius = 2f;

        [Header("Refs")]
        [SerializeField] private ZombieFatAnimator animator;
        [SerializeField] private BaseEnemyController controller;

        [Header("Impact")]
        [SerializeField] private GameObject impactEffectPrefab;
        [SerializeField] private LayerMask playerLayer;
        [SerializeField] private float endDelayAfterImpact = 0.5f;

        private Transform _target;
        private Coroutine _endRoutine;

        protected override void OnExecute(Transform target)
        {
            _target = target;
            if (animator != null) animator.TriggerJumpAttack();
        }

        public void OnImpact()
        {
            if (!_isExecuting) return;

            if (impactEffectPrefab != null)
                Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);

            Collider[] hits = playerLayer == 0
                ? Physics.OverlapSphere(transform.position, aoeRadius)
                : Physics.OverlapSphere(transform.position, aoeRadius, playerLayer);

            foreach (var hit in hits)
            {
                if (!hit.CompareTag("Player")) continue;

                var damageable = ResolveDamageable(hit.transform);
                if (damageable == null || damageable.IsDead) continue;

                damageable.TakeDamage(damage, gameObject);
            }

            if (_endRoutine != null) StopCoroutine(_endRoutine);
            _endRoutine = StartCoroutine(EndAfterDelay());
        }

        private IEnumerator EndAfterDelay()
        {
            yield return new WaitForSeconds(endDelayAfterImpact);
            MarkComplete();
            if (controller != null) controller.NotifySkillComplete();
            _endRoutine = null;
        }

        protected override void OnCancel()
        {
            if (_endRoutine != null)
            {
                StopCoroutine(_endRoutine);
                _endRoutine = null;
            }
            if (animator != null) animator.CancelAttack();
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