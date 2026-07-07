using UnityEngine;
using SimpleSurvival.Core;
using SimpleSurvival.Stats;

namespace SimpleSurvival.AI
{
    public sealed class SummonMinionSkill : BaseEnemySkill
    {
        [Header("Summon")]
        [SerializeField] private GameObject minionPrefab;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private GameObject summonEffectPrefab;

        [Header("HP Thresholds (fallback nếu không có ZombieWitchStatsConfig, giảm dần, vd 0.5 = 50% máu)")]
        [SerializeField] private float[] hpThresholds = { 0.5f };

        [Header("Refs")]
        [SerializeField] private BaseEnemyAnimator animator;
        [SerializeField] private BaseEnemyController controller;
        [SerializeField] private EnemyStats stats;

        private int _nextThresholdIndex;

        private void Start()
        {
            var witchConfig = stats?.EnemyConfig as ZombieWitchStatsConfig;
            if (witchConfig != null) hpThresholds = witchConfig.SummonHpThresholds;
            _nextThresholdIndex = 0;
        }

        public override bool IsAvailable(Transform target, float distanceToTarget)
        {
            if (_isExecuting) return false;
            if (target == null || stats == null || hpThresholds == null) return false;
            if (_nextThresholdIndex >= hpThresholds.Length) return false;
            if (stats.MaxHP <= 0f) return false;

            float hpPercent = stats.HP / stats.MaxHP;
            return hpPercent <= hpThresholds[_nextThresholdIndex];
        }

        protected override void OnExecute(Transform target)
        {
            _nextThresholdIndex++;

            if (animator != null)
            {
                animator.SetSummoning(true);
                animator.TriggerHowl();
                animator.OnHowlSpawn += HandleHowlSpawn;
                animator.OnHowlFinished += HandleHowlFinished;
            }
        }

        private void HandleHowlSpawn()
        {
            foreach (var point in spawnPoints)
                SpawnMinion(point);

            if (animator != null)
                animator.OnHowlSpawn -= HandleHowlSpawn;
        }

        private void HandleHowlFinished()
        {
            if (animator != null)
            {
                animator.SetSummoning(false);
                animator.OnHowlFinished -= HandleHowlFinished;
            }

            MarkComplete();
            if (controller != null) controller.NotifySkillComplete();
        }

        private void SpawnMinion(Transform point)
        {
            if (minionPrefab == null || point == null) return;

            if (summonEffectPrefab != null && ObjectPool.Instance != null)
            {
                var effect = ObjectPool.Instance.Get(summonEffectPrefab, point.position, Quaternion.identity);
                ObjectPool.Instance.ReturnDelayed(effect, 2f);
            }

            var obj = Instantiate(minionPrefab, point.position, point.rotation);
            var ctrl = obj.GetComponent<BaseEnemyController>();
            if (ctrl != null) ctrl.Initialize(null);
        }

        protected override void OnCancel()
        {
            if (animator != null)
            {
                animator.SetSummoning(false);
                animator.OnHowlSpawn -= HandleHowlSpawn;
                animator.OnHowlFinished -= HandleHowlFinished;
            }
        }
    }
}