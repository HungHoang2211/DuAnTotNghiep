using UnityEngine;
using System;

namespace SimpleSurvival.Stats
{
    public class HarvestStats : MonoBehaviour
    {
        public event Action<float, float> OnHPChanged;
        public event Action OnDepleted;

        [SerializeField] private HarvestStatsConfig config;

        public float HP { get; private set; }
        public float MaxHP => config != null ? config.MaxHP : 0f;
        public bool IsDepleted { get; private set; }

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogError($"[{name}] HarvestStats config is null.", this);
                return;
            }

            HP = config.MaxHP;
            IsDepleted = false;
        }

        public void TakeDamage(float amount)
        {
            if (IsDepleted || amount <= 0f || config == null) return;

            float prev = HP;
            HP = Mathf.Max(0f, HP - amount);

            if (!Mathf.Approximately(HP, prev))
                OnHPChanged?.Invoke(HP, MaxHP);

            if (HP <= 0f)
                Deplete();
        }

        private void Deplete()
        {
            IsDepleted = true;
            OnDepleted?.Invoke();
        }
    }
}