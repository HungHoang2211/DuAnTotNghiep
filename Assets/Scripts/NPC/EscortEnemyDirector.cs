using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleSurvival.Stats;

namespace SimpleSurvival.AI
{
    /// <summary>
    /// Điều phối enemy "ẩn" (spawnOnStart = false trên EnemySpawnPoint) chỉ được kích hoạt
    /// khi player nhận nhiệm vụ hộ tống Emily. Enemy xuất hiện LẦN LƯỢT — con này chết mới
    /// spawn con kế — và mỗi con chỉ bám/tấn công Emily, không đụng tới Player.
    ///
    /// Enemy spawn sẵn trên map từ trước (EnemySpawnPoint spawnOnStart = true) hoàn toàn
    /// KHÔNG bị ảnh hưởng bởi class này — chúng không được đăng ký vào hiddenSpawnPoints
    /// nên vẫn hoạt động như bình thường (chỉ nhắm Player).
    /// </summary>
    public sealed class EscortEnemyDirector : MonoBehaviour
    {
        [Header("Điểm spawn ẩn - đặt Spawn On Start = false, Auto Respawn = false trên từng điểm")]
        [SerializeField] private List<EnemySpawnPoint> hiddenSpawnPoints = new List<EnemySpawnPoint>();

        [Header("Thời gian nghỉ trước khi enemy kế tiếp xuất hiện (giây)")]
        [SerializeField] private float delayBetweenSpawns = 1.5f;

        private Transform _escortTarget;
        private int _spawnIndex;
        private bool _isActive;
        private EnemySpawnPoint _pendingPoint;
        private readonly List<BaseEnemyController> _liveEnemies = new List<BaseEnemyController>();

        /// <summary>
        /// Bắt đầu chuỗi spawn lần lượt, nhắm vào escortTarget (transform của Emily).
        /// </summary>
        public void BeginEncounter(Transform escortTarget)
        {
            _escortTarget = escortTarget;
            _spawnIndex = 0;
            _isActive = true;
            SpawnNext();
        }

        /// <summary>
        /// Dừng hẳn chuỗi spawn (gọi khi hộ tống thành công / thất bại / bị hủy).
        /// Enemy đã spawn trước đó vẫn tồn tại và tiếp tục hành xử theo escort mode hiện tại
        /// (thường sẽ tự dừng vì Emily không còn hoạt động hoặc đã bị ragdoll).
        /// </summary>
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

        /// <summary>
        /// Trả toàn bộ enemy hộ tống còn sống về chế độ bình thường (tự tìm & đánh Player thật).
        /// Gọi khi encounter kết thúc (Emily tới đích thành công, hoặc hộ tống thất bại).
        /// </summary>
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
            if (_spawnIndex >= hiddenSpawnPoints.Count) return; // hết danh sách, không spawn thêm

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
                // Không có EnemyStats thì không biết khi nào chết -> vẫn spawn tiếp theo delay mặc định
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