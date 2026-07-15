using UnityEngine;
using System;
using SimpleSurvival.Items;

namespace SimpleSurvival.Stats
{
    public sealed class PlayerStats : BaseStats
    {
        public event Action<float, float> OnHungerChanged;
        public event Action<float, float> OnThirstChanged;
        public event Action OnCombatStatsChanged;
        public event Action<ItemStack, EquipSlot> OnArmorBroken;
        public event Action OnRevived;
        public float Hunger { get; private set; }
        public float Thirst { get; private set; }
        public float MaxHunger => Config != null ? Config.MaxHunger : 0f;
        public float MaxThirst => Config != null ? Config.MaxThirst : 0f;

        [Header("Regen Settings")]
        [Tooltip("Allow HP natural regen in current map. Disable for combat maps.")]
        [SerializeField] private bool allowRegen = true;

        [Header("Decay Pause After Consume")]
        [Tooltip("Thời gian (giây) tạm dừng decay Hunger/Thirst sau khi consume item.")]
        [SerializeField] private float decayPauseAfterConsume = 5f;

        [Header("Equipment Reference")]
        [SerializeField] private PlayerEquipment playerEquipment;

        public bool AllowRegen
        {
            get => allowRegen;
            set => allowRegen = value;
        }

        private PlayerStatsConfig Config => baseConfig as PlayerStatsConfig;

        private float _hpRegenTimer;
        private float _starveTimer;
        private float _dehydrateTimer;
        private float _hungerPauseUntil;
        private float _thirstPauseUntil;

        private static readonly EquipSlot[] ArmorSlots =
        {
            EquipSlot.Helmet,
            EquipSlot.Jacket,
            EquipSlot.Pants,
            EquipSlot.Boots
        };

        public override float Armor => TotalDefense;

        public float TotalDamage
        {
            get
            {
                ItemStack weapon = GetEquipped(EquipSlot.Weapon);
                if (weapon != null && !weapon.IsBroken)
                {
                    WeaponAbility ability = weapon.ItemData.GetAbility<WeaponAbility>();
                    if (ability != null) return ability.Damage;
                }
                return BaseDamage;
            }
        }

        public float TotalAttackSpeed
        {
            get
            {
                ItemStack weapon = GetEquipped(EquipSlot.Weapon);
                if (weapon != null && !weapon.IsBroken)
                {
                    WeaponAbility ability = weapon.ItemData.GetAbility<WeaponAbility>();
                    if (ability != null) return ability.AttackSpeed;
                }
                return BaseAttackSpeed;
            }
        }

        public float TotalDefense
        {
            get
            {
                float sum = 0f;
                foreach (EquipSlot slot in ArmorSlots)
                    sum += GetArmorValue(slot);
                return sum;
            }
        }

        public float TotalMoveSpeed
        {
            get
            {
                float modifier = 1f;

                ItemStack boots = GetEquipped(EquipSlot.Boots);
                if (boots != null && !boots.IsBroken)
                {
                    EquipmentAbility ability = boots.ItemData.GetAbility<EquipmentAbility>();
                    if (ability != null) modifier += ability.SpeedBonus;
                }

                ItemStack weapon = GetEquipped(EquipSlot.Weapon);
                if (weapon != null && !weapon.IsBroken)
                {
                    WeaponAbility ability = weapon.ItemData.GetAbility<WeaponAbility>();
                    if (ability != null && Config != null)
                        modifier -= ability.Weight * Config.WeightSpeedFactor;
                }

                if (modifier < 0.1f) modifier = 0.1f;
                return MoveSpeed * modifier;
            }
        }


        public void Revive()
        {
            ResetStats();
            Debug.Log("[PlayerStats] Revive() called, invoking OnRevived");
            OnRevived?.Invoke();
        }
        protected override void Awake()
        {
            base.Awake();
            if (baseConfig != null && Config == null)
            {
                Debug.LogError($"[{name}] PlayerStats requires PlayerStatsConfig, got {baseConfig.GetType().Name}", this);
            }
            if (playerEquipment == null)
                playerEquipment = GetComponentInParent<PlayerEquipment>();
        }

        private void Start()
        {
            if (playerEquipment != null && playerEquipment.System != null)
                playerEquipment.System.OnSlotChanged += HandleEquipmentSlotChanged;
        }

        private void OnDestroy()
        {
            if (playerEquipment != null && playerEquipment.System != null)
                playerEquipment.System.OnSlotChanged -= HandleEquipmentSlotChanged;
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
            _hungerPauseUntil = 0f;
            _thirstPauseUntil = 0f;
            OnHungerChanged?.Invoke(Hunger, Config.MaxHunger);
            OnThirstChanged?.Invoke(Thirst, Config.MaxThirst);
            OnCombatStatsChanged?.Invoke();
        }

        public void RestoreSurvival(float hunger, float thirst)
        {
            if (Config == null) return;
            SetHunger(hunger);
            SetThirst(thirst);
        }

        protected override void OnPostDamage(float rawDamage, GameObject source)
        {
            ReduceArmorDurability();
        }

        private void ReduceArmorDurability()
        {
            if (playerEquipment == null || playerEquipment.System == null) return;

            foreach (EquipSlot slot in ArmorSlots)
            {
                ItemStack stack = playerEquipment.System.GetSlot(slot, 0);
                if (stack == null) continue;
                if (!stack.ItemData.IsDurable) continue;
                if (stack.IsBroken) continue;

                bool broke = stack.ReduceDurability();
                if (broke)
                {
                    Debug.Log($"[ArmorBroken] {stack.ItemData.ItemName} in {slot}");
                    playerEquipment.System.SetSlotDirect(slot, 0, null);
                    OnArmorBroken?.Invoke(stack, slot);
                }
            }
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

        public void PauseHungerDecay()
        {
            _hungerPauseUntil = Time.time + decayPauseAfterConsume;
        }

        public void PauseThirstDecay()
        {
            _thirstPauseUntil = Time.time + decayPauseAfterConsume;
        }

        private void TickHunger(float dt)
        {
            if (Time.time < _hungerPauseUntil) return;
            SetHunger(Hunger - Config.HungerDecayPerSec * dt);
        }

        private void TickThirst(float dt)
        {
            if (Time.time < _thirstPauseUntil) return;
            SetThirst(Thirst - Config.ThirstDecayPerSec * dt);
        }

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

        private void HandleEquipmentSlotChanged(EquipSlot slot, int index, ItemStack stack)
        {
            OnCombatStatsChanged?.Invoke();
        }

        private ItemStack GetEquipped(EquipSlot slot)
        {
            if (playerEquipment == null || playerEquipment.System == null) return null;
            return playerEquipment.System.GetSlot(slot, 0);
        }

        private float GetArmorValue(EquipSlot slot)
        {
            ItemStack stack = GetEquipped(slot);
            if (stack == null || stack.IsBroken) return 0f;

            EquipmentAbility ability = stack.ItemData.GetAbility<EquipmentAbility>();
            return ability != null ? ability.ArmorValue : 0f;
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