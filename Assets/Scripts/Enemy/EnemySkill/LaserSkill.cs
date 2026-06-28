using System.Collections;
using UnityEngine;
using SimpleSurvival.Core;

namespace SimpleSurvival.AI
{
    public sealed class LaserSkill : BaseEnemySkill
    {
        [Header("Projectile")]
        [SerializeField] private GameObject orbPrefab;

        [Header("Refs")]
        [SerializeField] private ZombieBossAnimator animator;
        [SerializeField] private BaseEnemyController controller;

        private Transform _target;
        private Coroutine _routine;

        protected override void OnExecute(Transform target)
        {
            _target = target;
            _routine = StartCoroutine(LaserRoutine());
        }

        private IEnumerator LaserRoutine()
        {
            float faceTime = 0.3f;
            float elapsed = 0f;

            while (elapsed < faceTime)
            {
                if (_target != null)
                {
                    Vector3 dir = _target.position - transform.position;
                    dir.y = 0;
                    if (dir != Vector3.zero)
                        transform.rotation = Quaternion.RotateTowards(
                            transform.rotation,
                            Quaternion.LookRotation(dir),
                            360f * Time.deltaTime);
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            ShootLaser();
            yield return new WaitForSeconds(0.2f);

            MarkComplete();
            if (controller != null) controller.NotifySkillComplete();
        }

        private void ShootLaser()
        {
            if (orbPrefab == null || _target == null) return;

            var obj = ObjectPool.Instance.Get(orbPrefab, _target.position, Quaternion.identity);
            var laser = obj.GetComponent<ZombieBossSkill>();
            if (laser != null)
                laser.Launch(_target.position, gameObject);
        }

        protected override void OnCancel()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
        }
    }
}