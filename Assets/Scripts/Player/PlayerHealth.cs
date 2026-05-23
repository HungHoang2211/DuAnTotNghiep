using UnityEngine;
using Xyla.Combat;

namespace Xyla.Player
{

    /// Trung gian giữa Enemy và SurvivalStats.
    /// Implement IDamageable để Enemy gọi TakeDamage() mà không cần biết bên trong Player dùng SurvivalStats.

    /// SETUP: Gắn lên Player cùng chỗ với SurvivalStats.

    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private SurvivalStats _stats;

        // IDamageable
        public float CurrentHealth => _stats != null ? _stats.HP : 0f;
        public bool IsDead => _stats != null && _stats.IsDead;

        private void Awake()
        {
            if (_stats == null)
                _stats = GetComponent<SurvivalStats>();
        }

        public bool TakeDamage(float amount, GameObject source)
        {
            if (_stats == null || _stats.IsDead) return false;

            _stats.TakeDamage(amount);

            Debug.Log($"[PlayerHealth] Nhận {amount} damage từ {source?.name}. " +
                      $"HP còn: {_stats.HP:F1}");

            return !_stats.IsDead;
        }
    }
}