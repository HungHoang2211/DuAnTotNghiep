using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleSurvival.Stats;

namespace SimpleSurvival.AI
{
    public sealed class EscortEnemyDirector : MonoBehaviour
    {
        [Header("Điểm spawn ẩn")]
        [SerializeField] private List<EnemySpawnPoint> hiddenSpawnPoints = new List<EnemySpawnPoint>();

        [Header("Thời gian enemy kế tiếp xuất hiện")]
        [SerializeField] private float delayBetweenSpawns = 1.5f;

        private Transform _escortTarget;
        private int _spawnIndex;
        private bool _isActive;
        private EnemySpawnPoint _pendingPoint;
        private readonly List<BaseEnemyController> _liveEnemies = new List<BaseEnemyController>();

        public void BeginEncounter(Transform escortTarget)
        {
            _escortTarget = escortTarget;
            _spawnIndex = 0;
            _isActive = true;
            SpawnNext();
        }

        public void StopEncounter()
        {
            _isActive = false;

            if (_pendingPoint != null)
            {
                _pendingPoint.OnEnemySpawned -= HandleEnemySpawned;
                _pendingPoint = null;
            }

            ReleaseAllToPlayer();
        }

        private void ReleaseAllToPlayer()
        {
            foreach (var enemy in _liveEnemies)
            {
                if (enemy == null) continue;
                enemy.ReleaseEscortTarget();
            }
            _liveEnemies.Clear();
        }

        private void SpawnNext()
        {
            if (!_isActive) return;
            if (_spawnIndex >= hiddenSpawnPoints.Count) return;

            EnemySpawnPoint point = hiddenSpawnPoints[_spawnIndex];
            _spawnIndex++;

            if (point == null)
            {
                SpawnNext();
                return;
            }

            _pendingPoint = point;
            point.OnEnemySpawned += HandleEnemySpawned;
            point.Spawn();
        }

        private void HandleEnemySpawned(GameObject enemyObject)
        {
            if (_pendingPoint != null)
            {
                _pendingPoint.OnEnemySpawned -= HandleEnemySpawned;
                _pendingPoint = null;
            }

            if (!_isActive || enemyObject == null) return;

            var controller = enemyObject.GetComponent<BaseEnemyController>();
            var stats = enemyObject.GetComponent<EnemyStats>();

            controller?.SetEscortTarget(_escortTarget);

            if (controller != null)
                _liveEnemies.Add(controller);

            if (stats == null)
            {
                StartCoroutine(SpawnNextAfterDelay());
                return;
            }

            void OnEscortEnemyDeath(GameObject source)
            {
                stats.OnDeath -= OnEscortEnemyDeath;
                if (controller != null) _liveEnemies.Remove(controller);
                if (_isActive) StartCoroutine(SpawnNextAfterDelay());
            }

            stats.OnDeath += OnEscortEnemyDeath;
        }

        private IEnumerator SpawnNextAfterDelay()
        {
            yield return new WaitForSeconds(delayBetweenSpawns);
            SpawnNext();
        }
    }
}