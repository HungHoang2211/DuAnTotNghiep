using System.Collections;
using UnityEngine;
using SimpleSurvival.Core;

namespace SimpleSurvival.AI
{
    public sealed class AcidAttackSkill : BaseEnemySkill
    {
        [Header("Damage")]
        [SerializeField] private float damage = 25f;

        [Header("Refs")]
        [SerializeField] private BaseEnemyAnimator animator;
        [SerializeField] private BaseEnemyController controller;

        [Header("Range Timing")]
        [Tooltip("Player phải đứng liên tục trong khoảng [Min Range, Max Range] đủ số giây này thì mới được phun acid")]
        [SerializeField] private float requiredTimeInRange = 3f;

        private float _timeInRange = 0f;

        [Header("Projectile")]
        [Tooltip("Prefab phải có component AcidProjectile gắn sẵn. Bắn thẳng theo hướng tới player tại thời điểm phun, có thể né được.")]
        [SerializeField] private GameObject acidEffectPrefab;
        [SerializeField] private Transform spawnPoint;

        [Header("Failsafe")]
        [Tooltip("Nếu quên gắn Animation Event OnAcidEnd ở cuối clip, skill sẽ tự kết thúc sau thời gian này để tránh kẹt state")]
        [SerializeField] private float failsafeDuration = 3f;

        private Transform _target;
        private Coroutine _failsafeRoutine;

        public override bool IsAvailable(Transform target, float distanceToTarget)
        {
            bool inRange =
                target != null &&
                distanceToTarget >= minRange &&
                distanceToTarget <= maxRange;

            if (inRange)
                _timeInRange += Time.deltaTime;
            else
                _timeInRange = 0f;

            if (!base.IsAvailable(target, distanceToTarget))
                return false;

            if (_timeInRange < requiredTimeInRange)
                return false;

            return true;
        }

        protected override void OnExecute(Transform target)
        {
            _target = target;
            _timeInRange = 0f;

            if (animator != null)
                animator.TriggerAcidAttack();

            if (_failsafeRoutine != null)
                StopCoroutine(_failsafeRoutine);

            _failsafeRoutine = StartCoroutine(FailsafeEndRoutine());
        }

        public void OnAcidSpit()
        {
            if (!_isExecuting)
                return;

            if (acidEffectPrefab != null && _target != null)
            {
                Vector3 spawnPos =
                    spawnPoint != null
                        ? spawnPoint.position
                        : transform.position + transform.forward;

                Vector3 dir =
                    (_target.position - spawnPos).normalized;

                Quaternion spawnRot =
                    dir != Vector3.zero
                        ? Quaternion.LookRotation(dir)
                        : transform.rotation;

                // Phát âm thanh khi acid được phun
                PlayHitSound();

                // Spawn acid projectile
                var go = ObjectPool.Instance.Get(
                    acidEffectPrefab,
                    spawnPos,
                    spawnRot
                );

                var projectile = go.GetComponent<AcidProjectile>();

                if (projectile != null)
                    projectile.Init(
                        damage,
                        gameObject,
                        controller
                    );
            }
        }

        public void OnAcidEnd()
        {
            if (!_isExecuting)
                return;

            if (_failsafeRoutine != null)
            {
                StopCoroutine(_failsafeRoutine);
                _failsafeRoutine = null;
            }

            MarkComplete();

            if (controller != null)
                controller.NotifySkillComplete();

            _target = null;
        }

        private IEnumerator FailsafeEndRoutine()
        {
            yield return new WaitForSeconds(failsafeDuration);

            OnAcidEnd();
        }

        protected override void OnCancel()
        {
            if (_failsafeRoutine != null)
            {
                StopCoroutine(_failsafeRoutine);
                _failsafeRoutine = null;
            }

            _timeInRange = 0f;

            if (animator != null)
                animator.CancelAttack();

            _target = null;
        }
    }
}
