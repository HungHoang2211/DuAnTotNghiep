using System;
using UnityEngine;

namespace SimpleSurvival.AI
{
    /// <summary>
    /// Gắn trên Trap_SpawnPoint_WitchEvent (hoặc 1 object quản lý riêng trong khu vực).
    /// Theo dõi 1 tập EnemySpawnPoint cố định (autoRespawn = false); khi tất cả enemy
    /// từ các spawn point này đã chết -> IsCleared = true.
    /// </summary>
    public class WitchEventEncounter : MonoBehaviour
    {
        [Header("Spawn Points thuộc encounter này")]
        [Tooltip("Các EnemySpawnPoint cố định (nhớ tick Auto Respawn = false trên từng cái)")]
        [SerializeField] private EnemySpawnPoint[] spawnPoints;

        public bool IsCleared { get; private set; }
        public event Action OnCleared;

        private int _remaining;

        private void Awake()
        {
            _remaining = spawnPoints != null ? spawnPoints.Length : 0;

            if (spawnPoints != null)
            {
                foreach (var sp in spawnPoints)
                    if (sp != null) sp.OnEnemyDefeated += HandleEnemyDefeated;
            }

            if (_remaining <= 0) MarkCleared();
        }

        private void OnDestroy()
        {
            if (spawnPoints == null) return;
            foreach (var sp in spawnPoints)
                if (sp != null) sp.OnEnemyDefeated -= HandleEnemyDefeated;
        }

        private void HandleEnemyDefeated()
        {
            if (IsCleared) return;

            _remaining--;
            if (_remaining <= 0) MarkCleared();
        }

        private void MarkCleared()
        {
            IsCleared = true;
            OnCleared?.Invoke();
        }
    }
}