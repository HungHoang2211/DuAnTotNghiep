using System;
using UnityEngine;

namespace SimpleSurvival.AI
{
    /// <summary>
    /// Hệ thống máu tối giản cho Emily trong lúc hộ tống.
    /// LƯU Ý: TakeDamage() ở đây là placeholder - cần khớp với cách enemy skill hiện tại
    /// đang gây damage lên Transform mục tiêu (xem phần "cần thêm file" trong câu trả lời).
    /// </summary>
    public sealed class NPCEmilyStats : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 50f;

        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; private set; }
        public bool IsDead { get; private set; }

        public event Action<GameObject> OnDamagedBy;
        public event Action<GameObject> OnDeath;

        private void Awake()
        {
            CurrentHealth = maxHealth;
        }

        public void ResetStats()
        {
            CurrentHealth = maxHealth;
            IsDead = false;
        }

        public void TakeDamage(float amount, GameObject source)
        {
            if (IsDead || amount <= 0f) return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            OnDamagedBy?.Invoke(source);

            if (CurrentHealth <= 0f)
            {
                IsDead = true;
                OnDeath?.Invoke(source);
            }
        }
    }
}