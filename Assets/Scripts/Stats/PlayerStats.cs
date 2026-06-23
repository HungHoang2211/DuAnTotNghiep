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

        [Header("Regen Settings")]
        [Tooltip("Allow HP natural regen in current map. Disable for combat maps.")]
        [SerializeField] private bool allowRegen = true;

        public bool AllowRegen
        {
            get => allowRegen;
            set => allowRegen = value;
        }

        private PlayerStatsConfig Config => baseConfig as PlayerStatsConfig;

        private float _hpRegenTimer;
        private float _starveTimer;
        private float _dehydrateTimer;

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
            _hpRegenTimer = 0f;
            _starveTimer = 0f;
            _dehydrateTimer = 0f;
            OnHungerChanged?.Invoke(Hunger, Config.MaxHunger);
            OnThirstChanged?.Invoke(Thirst, Config.MaxThirst);
        }

        public void RestoreSurvival(float hunger, float thirst)
        {
            if (Config == null) return;
            SetHunger(hunger);
            SetThirst(thirst);
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
            if (!allowRegen || HP >= MaxHP)
            {
                _hpRegenTimer = 0f;
                return;
            }

            _hpRegenTimer += dt;
            if (_hpRegenTimer >= Config.HPRegenInterval)
            {
                Heal(Config.HPRegenAmount);
                _hpRegenTimer -= Config.HPRegenInterval;
            }
        }

        private void TickStarvation(float dt)
        {
            if (Hunger <= 0f)
            {
                _starveTimer += dt;
                if (_starveTimer >= Config.StarveDamageInterval)
                {
                    TakeDamage(Config.StarveDamageAmount);
                    _starveTimer -= Config.StarveDamageInterval;
                }
            }
            else
            {
                _starveTimer = 0f;
            }

            if (Thirst <= 0f)
            {
                _dehydrateTimer += dt;
                if (_dehydrateTimer >= Config.DehydrateDamageInterval)
                {
                    TakeDamage(Config.DehydrateDamageAmount);
                    _dehydrateTimer -= Config.DehydrateDamageInterval;
                }
            }
            else
            {
                _dehydrateTimer = 0f;
            }
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