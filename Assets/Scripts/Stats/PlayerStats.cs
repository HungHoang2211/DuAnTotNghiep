using UnityEngine;
using System;

namespace SimpleSurvival.Stats
{
    public sealed class PlayerStats : BaseStats
    {
        public event Action<float, float> OnHungerChanged;
        public event Action<float, float> OnThirstChanged;

        public float Hunger { get; private set; }
        public float Thirst { get; private set; }

        public float MaxHunger => Config != null ? Config.MaxHunger : 0f;
        public float MaxThirst => Config != null ? Config.MaxThirst : 0f;

        private PlayerStatsConfig Config => baseConfig as PlayerStatsConfig;

        protected override void Awake()
        {
            base.Awake();

            if (baseConfig != null && Config == null)
            {
                Debug.LogError($"[{name}] PlayerStats requires PlayerStatsConfig, got {baseConfig.GetType().Name}", this);
            }
        }

        public override void ResetStats()
        {
            base.ResetStats();

            if (Config == null) return;

            Hunger = Mathf.Clamp(Config.StartHunger, 0f, Config.MaxHunger);
            Thirst = Mathf.Clamp(Config.StartThirst, 0f, Config.MaxThirst);

            OnHungerChanged?.Invoke(Hunger, Config.MaxHunger);
            OnThirstChanged?.Invoke(Thirst, Config.MaxThirst);
        }

        private void Update()
        {
            if (!IsAlive || Config == null)
                return;

            float dt = Time.deltaTime;
            TickHunger(dt);
            TickThirst(dt);
            TickHPRegen(dt);
            TickStarvation(dt);
        }

        public void AddHunger(float amount)
        {
            if (amount <= 0f) return;
            SetHunger(Hunger + amount);
        }

        public void AddThirst(float amount)
        {
            if (amount <= 0f) return;
            SetThirst(Thirst + amount);
        }

        private void TickHunger(float dt) => SetHunger(Hunger - Config.HungerDecayPerSec * dt);
        private void TickThirst(float dt) => SetThirst(Thirst - Config.ThirstDecayPerSec * dt);

        private void TickHPRegen(float dt)
        {
            if (HP >= MaxHP) return;

            bool hungerOk = Hunger / Config.MaxHunger >= Config.RegenThreshold;
            bool thirstOk = Thirst / Config.MaxThirst >= Config.RegenThreshold;
            if (!hungerOk || !thirstOk) return;

            float regenAmount = Config.HPRegenPerSec * dt;
            float hungerCost = Config.HungerCostPerHPRegen * regenAmount;
            float thirstCost = Config.ThirstCostPerHPRegen * regenAmount;

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
                TakeDamage(Config.StarveDamagePerSec * dt);

            if (Thirst <= 0f)
                TakeDamage(Config.DehydrateDamagePerSec * dt);
        }

        private void SetHunger(float value)
        {
            float prev = Hunger;
            Hunger = Mathf.Clamp(value, 0f, Config.MaxHunger);

            if (!Mathf.Approximately(Hunger, prev))
                OnHungerChanged?.Invoke(Hunger, Config.MaxHunger);
        }

        private void SetThirst(float value)
        {
            float prev = Thirst;
            Thirst = Mathf.Clamp(value, 0f, Config.MaxThirst);

            if (!Mathf.Approximately(Thirst, prev))
                OnThirstChanged?.Invoke(Thirst, Config.MaxThirst);
        }
    }
}