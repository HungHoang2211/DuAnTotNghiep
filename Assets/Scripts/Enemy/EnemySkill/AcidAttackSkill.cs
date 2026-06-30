using System.Collections;
using UnityEngine;
using SimpleSurvival.Combat;

namespace SimpleSurvival.AI
{
    public sealed class AcidAttackSkill : BaseEnemySkill
    {
        [Header("Damage")]
        [SerializeField] private float damage = 25f;
        [SerializeField] private float coneRange = 4f;
        [SerializeField] private float coneAngle = 60f;

        [Header("Refs")]
        [SerializeField] private ZombieFatAnimator animator;
        [SerializeField] private BaseEnemyController controller;

        [Header("Effect")]
        [SerializeField] private GameObject acidEffectPrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private LayerMask playerLayer;
        [SerializeField] private float endDelayAfterImpact = 0.5f;

        private Transform _target;
        private Coroutine _endRoutine;

        protected override void OnExecute(Transform target)
        {
            _target = target;
            if (animator != null) animator.TriggerAcidAttack();
        }

        public void OnAcidSpit()
        {
            if (!_isExecuting) return;

            if (acidEffectPrefab != null)
            {
                Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : transform.position + transform.forward;
                Quaternion spawnRot = spawnPoint != null ? spawnPoint.rotation : transform.rotation;
                Instantiate(acidEffectPrefab, spawnPos, spawnRot);
            }

            Collider[] hits = playerLayer == 0
                ? Physics.OverlapSphere(transform.position, coneRange)
                : Physics.OverlapSphere(transform.position, coneRange, playerLayer);

            foreach (var hit in hits)
            {
                if (!hit.CompareTag("Player")) continue;

                Vector3 dir = (hit.transform.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, dir);
                if (angle > coneAngle * 0.5f) continue;

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