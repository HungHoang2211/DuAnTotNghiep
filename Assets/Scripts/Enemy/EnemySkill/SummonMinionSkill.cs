using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using SimpleSurvival.Core;
using SimpleSurvival.Stats;

namespace SimpleSurvival.AI
{
    public sealed class SummonMinionSkill : BaseEnemySkill
    {
        /// <summary>
        /// Mỗi group = 1 minion sẽ được spawn trong 1 lần triệu hồi.
        /// Nếu Prefabs có nhiều hơn 1 phần tử, sẽ random chọn 1 loại cho group đó.
        /// </summary>
        [System.Serializable]
        public class MinionSpawnGroup
        {
            [Tooltip("Danh sách enemy prefab có thể ra ở group này. 1 phần tử = luôn ra loại đó. Nhiều phần tử = random 1 trong số đó.")]
            public GameObject[] Prefabs;
        }

        [Header("Summon")]
        [Tooltip("Số minion sẽ spawn mỗi lần triệu hồi, random vị trí trên toàn bộ NavMesh của map.")]
        [SerializeField] private MinionSpawnGroup[] _minionsToSummon;
        [SerializeField] private GameObject summonEffectPrefab;

        [Header("HP Thresholds (fallback nếu không có ZombieWitchStatsConfig, giảm dần, vd 0.5 = 50% máu)")]
        [SerializeField] private float[] hpThresholds = { 0.5f };

        [Header("Refs")]
        [SerializeField] private BaseEnemyAnimator animator;
        [SerializeField] private BaseEnemyController controller;
        [SerializeField] private EnemyStats stats;

        private int _nextThresholdIndex;
        private readonly List<EnemyStats> _aliveMinions = new List<EnemyStats>();

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
            if (_minionsToSummon != null && _minionsToSummon.Length > 0)
            {
                // Tính tam giác NavMesh 1 lần cho cả đợt triệu hồi, tránh gọi
                // CalculateTriangulation() (khá nặng) lặp lại cho từng minion.
                var sampler = new NavMeshAreaSampler();
                foreach (var group in _minionsToSummon)
                    SpawnMinion(group, sampler);
            }

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

            if (controller == null) return;

            // Nếu vì lý do gì đó không có minion nào sống sót (spawn fail, config rỗng...)
            // thì witch đánh tiếp bình thường thay vì lui về ẩn vô thời hạn.
            if (_aliveMinions.Count > 0)
                controller.BeginRetreat();
            else
                controller.NotifySkillComplete();
        }

        private void SpawnMinion(MinionSpawnGroup group, NavMeshAreaSampler sampler)
        {
            if (group == null || group.Prefabs == null || group.Prefabs.Length == 0) return;

            GameObject prefab = PickRandomPrefab(group.Prefabs);
            if (prefab == null) return;

            if (!sampler.TryGetRandomPoint(out Vector3 spawnPos))
                spawnPos = transform.position; // fallback nếu map chưa bake NavMesh

            Quaternion spawnRot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            if (summonEffectPrefab != null && ObjectPool.Instance != null)
            {
                var effect = ObjectPool.Instance.Get(summonEffectPrefab, spawnPos, Quaternion.identity);
                ObjectPool.Instance.ReturnDelayed(effect, 2f);
            }

            var obj = Instantiate(prefab, spawnPos, spawnRot);

            var ctrl = obj.GetComponent<BaseEnemyController>();
            if (ctrl != null) ctrl.Initialize(null);

            TrackMinion(obj);
        }

        private void TrackMinion(GameObject minionObj)
        {
            var minionStats = minionObj.GetComponent<EnemyStats>();
            if (minionStats == null) return;

            _aliveMinions.Add(minionStats);
            minionStats.OnDeath += (_) => HandleMinionDeath(minionStats);
        }

        private void HandleMinionDeath(EnemyStats minionStats)
        {
            _aliveMinions.Remove(minionStats);

            // Chỉ khi tiêu diệt hết toàn bộ minion của đợt triệu hồi này,
            // witch mới hiện lại và quay lại tấn công player.
            if (_aliveMinions.Count == 0 && controller != null)
                controller.ReappearAndResume();
        }

        private GameObject PickRandomPrefab(GameObject[] prefabs)
        {
            if (prefabs.Length == 1) return prefabs[0];

            // Bỏ qua slot null trong lúc random để tránh instantiate lỗi
            int tries = prefabs.Length * 2;
            while (tries-- > 0)
            {
                int index = Random.Range(0, prefabs.Length);
                if (prefabs[index] != null) return prefabs[index];
            }
            return null;
        }

        protected override void OnCancel()
        {
            if (animator != null)
            {
                animator.SetSummoning(false);
                animator.OnHowlSpawn -= HandleHowlSpawn;
                animator.OnHowlFinished -= HandleHowlFinished;
            }

            _aliveMinions.Clear();
        }

        /// <summary>
        /// Cache tam giác của NavMesh đã bake để random điểm đều trên toàn bộ map
        /// (weighted theo diện tích tam giác, tránh dồn điểm ở vùng tam giác nhỏ).
        /// </summary>
        private class NavMeshAreaSampler
        {
            private readonly Vector3[] _vertices;
            private readonly int[] _indices;
            private readonly float[] _cumulativeAreas;
            private readonly float _totalArea;

            public NavMeshAreaSampler()
            {
                NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
                _vertices = tri.vertices;
                _indices = tri.indices;

                int triangleCount = _indices.Length / 3;
                _cumulativeAreas = new float[triangleCount];
                float running = 0f;

                for (int i = 0; i < triangleCount; i++)
                {
                    Vector3 a = _vertices[_indices[i * 3]];
                    Vector3 b = _vertices[_indices[i * 3 + 1]];
                    Vector3 c = _vertices[_indices[i * 3 + 2]];
                    running += Vector3.Cross(b - a, c - a).magnitude * 0.5f;
                    _cumulativeAreas[i] = running;
                }

                _totalArea = running;
            }

            public bool TryGetRandomPoint(out Vector3 point)
            {
                if (_totalArea <= 0f)
                {
                    point = Vector3.zero;
                    return false;
                }

                float roll = Random.Range(0f, _totalArea);
                int triangleIndex = System.Array.BinarySearch(_cumulativeAreas, roll);
                if (triangleIndex < 0) triangleIndex = ~triangleIndex;
                triangleIndex = Mathf.Clamp(triangleIndex, 0, _cumulativeAreas.Length - 1);

                Vector3 a = _vertices[_indices[triangleIndex * 3]];
                Vector3 b = _vertices[_indices[triangleIndex * 3 + 1]];
                Vector3 c = _vertices[_indices[triangleIndex * 3 + 2]];

                float r1 = Random.value;
                float r2 = Random.value;
                if (r1 + r2 > 1f)
                {
                    r1 = 1f - r1;
                    r2 = 1f - r2;
                }

                point = a + r1 * (b - a) + r2 * (c - a);
                return true;
            }
        }
    }
}