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

        [Header("Range Timing")]
        [Tooltip("Player phải đứng liên tục trong khoảng [Min Range, Max Range] đủ số giây này thì mới được phun acid")]
        [SerializeField] private float requiredTimeInRange = 3f;

        private float _timeInRange = 0f;

        [Header("Effect")]
        [SerializeField] private GameObject acidEffectPrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private LayerMask playerLayer;

        [Header("Failsafe")]
        [Tooltip("Nếu quên gắn Animation Event OnAcidEnd ở cuối clip, skill sẽ tự kết thúc sau thời gian này để tránh kẹt state")]
        [SerializeField] private float failsafeDuration = 3f;

        private Transform _target;
        private Coroutine _failsafeRoutine;

        public override bool IsAvailable(Transform target, float distanceToTarget)
        {
            // Chỉ tính giờ khi player thực sự đứng trong khoảng [minRange, maxRange]
            // (minRange/maxRange chỉnh trong Inspector = "khoảng cách nhất định" bạn muốn).
            bool inRange = target != null && distanceToTarget >= minRange && distanceToTarget <= maxRange;

            if (inRange) _timeInRange += Time.deltaTime;
            else _timeInRange = 0f;

            if (!base.IsAvailable(target, distanceToTarget)) return false;
            if (_timeInRange < requiredTimeInRange) return false;

            return true;
        }

        protected override void OnExecute(Transform target)
        {
            _target = target;
            _timeInRange = 0f; // dùng xong reset, lần sau player phải đứng đủ giờ lại từ đầu
            if (animator != null) animator.TriggerAcidAttack();

            if (_failsafeRoutine != null) StopCoroutine(_failsafeRoutine);
            _failsafeRoutine = StartCoroutine(FailsafeEndRoutine());
        }

        /// <summary>Gắn Animation Event tại frame acid rời khỏi miệng zombie (giữa clip AcidAttack).</summary>
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
        }

        /// <summary>Gắn Animation Event ở FRAME CUỐI clip AcidAttack (khi animation kết thúc hẳn).</summary>
        public void OnAcidEnd()
        {
            if (!_isExecuting) return;

            if (_failsafeRoutine != null)
            {
                StopCoroutine(_failsafeRoutine);
                _failsafeRoutine = null;
            }

            MarkComplete();
            if (controller != null) controller.NotifySkillComplete();
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