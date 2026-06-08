using UnityEngine;
using System;

namespace SimpleSurvival.Stats
{
    /// <summary>
    /// Chỉ số của player — mở rộng BaseStats với Hunger, Thirst và HP regen.
    /// Anh ta implement logic decay/regen vào đây thay vì SurvivalStats cũ.
    ///
    /// Combat system đọc BaseDamage/BaseAttackSpeed từ base khi không equip vũ khí.
    /// Equipment system gọi SetArmor() khi thay đổi trang bị.
    /// ItemActionPanel subscribe OnUseConsumableRequested → gọi AddHunger/AddThirst/Heal.
    /// </summary>
    public sealed class PlayerStats : BaseStats
    {
        public event Action<float, float> OnHungerChanged;
        public event Action<float, float> OnThirstChanged;

        [Header("Hunger")]
        [SerializeField] private float maxHunger = 100f;
        [SerializeField] private float startHunger = 100f;

        [Tooltip("Hunger giảm mỗi giây.")]
        [SerializeField] private float hungerDecayPerSec = 0.01f;

        [Tooltip("HP giảm mỗi giây khi Hunger = 0.")]
        [SerializeField] private float starveDamagePerSec = 1f;

        [Header("Thirst")]
        [SerializeField] private float maxThirst = 100f;
        [SerializeField] private float startThirst = 100f;

        [Tooltip("Thirst giảm mỗi giây.")]
        [SerializeField] private float thirstDecayPerSec = 0.03f;

        [Tooltip("HP giảm mỗi giây khi Thirst = 0.")]
        [SerializeField] private float dehydrateDamagePerSec = 1.5f;

        [Header("HP Regen")]
        [Tooltip("HP hồi mỗi giây khi Hunger và Thirst đều trên ngưỡng.")]
        [SerializeField] private float hpRegenPerSec = 2f;

        [Tooltip("Ngưỡng tối thiểu để hồi HP (0-1, ví dụ 0.2 = 20%).")]
        [SerializeField, Range(0f, 1f)] private float regenThreshold = 0.2f;

        [Tooltip("Hunger tiêu hao mỗi 1 HP được hồi.")]
        [SerializeField] private float hungerCostPerHPRegen = 0.5f;

        [Tooltip("Thirst tiêu hao mỗi 1 HP được hồi.")]
        [SerializeField] private float thirstCostPerHPRegen = 0.8f;

        public float Hunger { get; private set; }
        public float Thirst { get; private set; }
        public float MaxHunger => maxHunger;
        public float MaxThirst => maxThirst;

        // ── Lifecycle ────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            Hunger = Mathf.Clamp(startHunger, 0f, maxHunger);
            Thirst = Mathf.Clamp(startThirst, 0f, maxThirst);
        }

        private void Update()
        {
            if (!IsAlive)
                return;

            float dt = Time.deltaTime;
            TickHunger(dt);
            TickThirst(dt);
            TickHPRegen(dt);
            TickStarvation(dt);
        }

        // ── Public API ───────────────────────────────────────────────────────
        public void AddHunger(float amount)
        {
            if (amount <= 0f)
                return;

            SetHunger(Hunger + amount);
        }

        public void AddThirst(float amount)
        {
            if (amount <= 0f)
                return;

            SetThirst(Thirst + amount);
        }

        private void TickHunger(float dt)
        {
            SetHunger(Hunger - hungerDecayPerSec * dt);
        }

        private void TickThirst(float dt)
        {
            SetThirst(Thirst - thirstDecayPerSec * dt);
        }

        private void TickHPRegen(float dt)
        {
            if (HP >= MaxHP)
                return;

            bool hungerOk = Hunger / maxHunger >= regenThreshold;
            bool thirstOk = Thirst / maxThirst >= regenThreshold;
            if (!hungerOk || !thirstOk)
                return;

            float regenAmount = hpRegenPerSec * dt;
            float hungerCost = hungerCostPerHPRegen * regenAmount;
            float thirstCost = thirstCostPerHPRegen * regenAmount;

            float scale = Mathf.Min(
                hungerCost > 0f ? Hunger / hungerCost : 1f,
                thirstCost > 0f ? Thirst / thirstCost : 1f,
                1f);

            Heal(regenAmount * scale);
            SetHunger(Hunger - hungerCost * scale);
            SetThirst(Thirst - thirstCost * scale);
        }

        private void TickStarvation(float dt)
        {
            if (Hunger <= 0f)
                TakeDamage(starveDamagePerSec * dt);

            if (Thirst <= 0f)
                TakeDamage(dehydrateDamagePerSec * dt);
        }


        private void SetHunger(float value)
        {
            float prev = Hunger;
            Hunger = Mathf.Clamp(value, 0f, maxHunger);

            if (!Mathf.Approximately(Hunger, prev))
                OnHungerChanged?.Invoke(Hunger, maxHunger);
        }

        private void SetThirst(float value)
        {
            float prev = Thirst;
            Thirst = Mathf.Clamp(value, 0f, maxThirst);

            if (!Mathf.Approximately(Thirst, prev))
                OnThirstChanged?.Invoke(Thirst, maxThirst);
        }
    }
}