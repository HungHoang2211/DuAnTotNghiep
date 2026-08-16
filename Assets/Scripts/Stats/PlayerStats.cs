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

        [Header("Weakened Debuff (Low HP / Hunger / Thirst)")]
        [Tooltip("Bật: khi HP dưới LowHpThreshold, hoặc Hunger/Thirst dưới ngưỡng thấp (Config), player bị giảm damage, " +
            "giảm Range + AttackSpeed vũ khí tầm xa, giảm MoveSpeed, và tăng tốc decay Hunger/Thirst. " +
            "Tắt tick này để vô hiệu hoá TOÀN BỘ nhóm debuff này — không cần sửa code.")]
        [SerializeField] private bool enableWeakenedPenalty = true;

        [Header("Medium HP Decay Penalty")]
        [Tooltip("Bật: khi HP dưới ngưỡng MediumHpThreshold (Config, mặc định 75%), Hunger/Thirst giảm nhanh gấp đôi thêm. " +
            "Tắt tick này để vô hiệu hoá tính năng — không cần sửa code.")]
        [SerializeField] private bool enableMediumHpDecayPenalty = true;

        [Header("Equipment Reference")]
        [SerializeField] private PlayerEquipment playerEquipment;

        public bool AllowRegen
        {
            get => allowRegen;
            set => allowRegen = value;
        }

        public bool EnableWeakenedPenalty
        {
            get => enableWeakenedPenalty;
            set => enableWeakenedPenalty = value;
        }

        public bool EnableMediumHpDecayPenalty
        {
            get => enableMediumHpDecayPenalty;
            set => enableMediumHpDecayPenalty = value;
        }

        private PlayerStatsConfig Config => baseConfig as PlayerStatsConfig;

        private float _hpRegenTimer;
        private float _starveTimer;
        private float _dehydrateTimer;
        private float _hungerPauseUntil;
        private float _thirstPauseUntil;
        private bool _wasWeakened;

        private static readonly EquipSlot[] ArmorSlots =
        {
            EquipSlot.Helmet,
            EquipSlot.Jacket,
            EquipSlot.Pants,
            EquipSlot.Boots
        };

        public override float Armor => TotalDefense;

        /// <summary>
        /// True khi HP hiện tại dưới ngưỡng LowHpThreshold (% MaxHP) trong PlayerStatsConfig.
        /// </summary>
        public bool IsLowHp => Config != null && MaxHP > 0f && HP < MaxHP * Config.LowHpThreshold;

        /// <summary>
        /// True khi HP hiện tại dưới ngưỡng MediumHpThreshold (% MaxHP, mặc định 75% — cao hơn LowHpThreshold).
        /// Chỉ dùng để tăng tốc decay Hunger/Thirst (xem TickHunger/TickThirst), không ảnh hưởng damage/range/movespeed.
        /// Có thể tắt hoàn toàn qua tick EnableMediumHpDecayPenalty trong Inspector.
        /// </summary>
        public bool IsMediumLowHp => Config != null && MaxHP > 0f && HP < MaxHP * Config.MediumHpThreshold;

        /// <summary>
        /// True khi Hunger hiện tại <= ngưỡng LowHungerThreshold (% MaxHunger) trong PlayerStatsConfig.
        /// </summary>
        public bool IsHungerLow => Config != null && MaxHunger > 0f && Hunger <= Config.LowHungerThreshold * MaxHunger;

        /// <summary>
        /// True khi Thirst hiện tại <= ngưỡng LowThirstThreshold (% MaxThirst) trong PlayerStatsConfig.
        /// </summary>
        public bool IsThirstLow => Config != null && MaxThirst > 0f && Thirst <= Config.LowThirstThreshold * MaxThirst;

        /// <summary>
        /// True khi (EnableWeakenedPenalty đang bật) VÀ HP thấp (dưới LowHpThreshold), HOẶC Hunger thấp
        /// (<= LowHungerThreshold), HOẶC Thirst thấp (<= LowThirstThreshold). Khi true: giảm damage (melee/base),
        /// giảm range + attack speed vũ khí tầm xa, giảm move speed, và tăng tốc độ giảm Hunger/Thirst.
        /// Tắt tick EnableWeakenedPenalty trong Inspector để vô hiệu hoá toàn bộ nhóm debuff này.
        /// </summary>
        public bool IsWeakened => enableWeakenedPenalty && (IsLowHp || IsHungerLow || IsThirstLow);

        public float TotalDamage
        {
            get
            {
                float dmg = BaseDamage;
                bool isRanged = false;

                ItemStack weapon = GetEquipped(EquipSlot.Weapon);
                if (weapon != null && !weapon.IsBroken)
                {
                    WeaponAbility ability = weapon.ItemData.GetAbility<WeaponAbility>();
                    if (ability != null)
                    {
                        dmg = ability.Damage;
                        isRanged = IsRangedCategory(ability.Category);
                    }
                }

                // HP/Hunger/Thirst thấp: giảm damage cho vũ khí cận chiến và base damage (unarmed).
                // Vũ khí tầm xa không bị giảm damage ở đây — thay vào đó bị giảm Range + AttackSpeed.
                if (!isRanged && IsWeakened && Config != null)
                    dmg *= Config.LowHpMeleeDamageMultiplier;

                return dmg;
            }
        }

        public float TotalAttackSpeed
        {
            get
            {
                float atkSpeed = BaseAttackSpeed;
                bool isRanged = false;

                ItemStack weapon = GetEquipped(EquipSlot.Weapon);
                if (weapon != null && !weapon.IsBroken)
                {
                    WeaponAbility ability = weapon.ItemData.GetAbility<WeaponAbility>();
                    if (ability != null)
                    {
                        atkSpeed = ability.AttackSpeed;
                        isRanged = IsRangedCategory(ability.Category);
                    }
                }

                // HP/Hunger/Thirst thấp: vũ khí tầm xa (Pistol/Rifle) bắn chậm hơn, cộng thêm với việc giảm Range ở TotalRange.
                if (isRanged && IsWeakened && Config != null)
                    atkSpeed *= Config.LowHpRangedAttackSpeedMultiplier;

                return atkSpeed;
            }
        }

        /// <summary>
        /// Range hiệu dụng của vũ khí đang trang bị. WeaponAbility chưa có stat "hitrate" riêng cho vũ khí
        /// tầm xa, nên khi HP/Hunger/Thirst thấp, Range bị giảm thay cho hitrate (vũ khí cận chiến không bị ảnh hưởng).
        /// </summary>
        public float TotalRange
        {
            get
            {
                ItemStack weapon = GetEquipped(EquipSlot.Weapon);
                if (weapon == null || weapon.IsBroken) return 0f;

                WeaponAbility ability = weapon.ItemData.GetAbility<WeaponAbility>();
                if (ability == null) return 0f;

                float range = ability.Range;
                if (IsWeakened && Config != null && IsRangedCategory(ability.Category))
                    range *= Config.LowHpRangedRangeMultiplier;

                return range;
            }
        }

        /// <summary>
        /// Tầm xa: Pistol, Rifle. Tầm gần: Melee1H, Melee2H, Fist.
        /// </summary>
        private static bool IsRangedCategory(WeaponCategory category)
        {
            return category == WeaponCategory.Pistol || category == WeaponCategory.Rifle;
        }

        /// <summary>
        /// Hệ số nhân damage do HP/Hunger/Thirst thấp, dành cho nơi khác (vd PlayerActionController)
        /// tự resolve damage/range/attackspeed từ WeaponAbility nhưng vẫn muốn áp penalty này.
        /// category = null nghĩa là unarmed (base damage) — vẫn tính là "cận chiến".
        /// Vũ khí tầm xa (Pistol/Rifle) KHÔNG bị giảm damage ở đây (xem GetRangeMultiplier/GetAttackSpeedMultiplier).
        /// </summary>
        public float GetDamageMultiplier(WeaponCategory? category)
        {
            if (Config == null || !IsWeakened) return 1f;
            bool isRanged = category.HasValue && IsRangedCategory(category.Value);
            return isRanged ? 1f : Config.LowHpMeleeDamageMultiplier;
        }

        /// <summary>
        /// Hệ số nhân Range do HP/Hunger/Thirst thấp. Chỉ áp dụng cho vũ khí tầm xa (Pistol/Rifle).
        /// </summary>
        public float GetRangeMultiplier(WeaponCategory? category)
        {
            if (Config == null || !IsWeakened) return 1f;
            bool isRanged = category.HasValue && IsRangedCategory(category.Value);
            return isRanged ? Config.LowHpRangedRangeMultiplier : 1f;
        }

        /// <summary>
        /// Hệ số nhân AttackSpeed (tốc độ bắn) do HP/Hunger/Thirst thấp. Chỉ áp dụng cho vũ khí tầm xa (Pistol/Rifle).
        /// </summary>
        public float GetAttackSpeedMultiplier(WeaponCategory? category)
        {
            if (Config == null || !IsWeakened) return 1f;
            bool isRanged = category.HasValue && IsRangedCategory(category.Value);
            return isRanged ? Config.LowHpRangedAttackSpeedMultiplier : 1f;
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

                if (IsWeakened && Config != null)
                    modifier *= Config.LowHpMoveSpeedMultiplier;

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
            _wasWeakened = IsWeakened;
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

            // TotalDamage/TotalRange/TotalAttackSpeed/TotalMoveSpeed phụ thuộc IsWeakened (HP/Hunger/Thirst).
            // Các giá trị này tự đổi theo từng frame do decay, nhưng UI (StatLabel...) chỉ refresh khi
            // OnCombatStatsChanged bắn ra — trước đây event này chỉ bắn khi đổi trang bị, nên UI bị "đứng hình"
            // cho tới khi tắt/bật lại panel. Ở đây chỉ bắn event khi trạng thái Weakened THỰC SỰ đổi (chuyển
            // true<->false), tránh spam event mỗi frame trong lúc Hunger/Thirst đang decay liên tục.
            bool weakenedNow = IsWeakened;
            if (weakenedNow != _wasWeakened)
            {
                _wasWeakened = weakenedNow;
                OnCombatStatsChanged?.Invoke();
            }
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
            float decayPerSec = Config.HungerDecayPerSec;
            if (IsWeakened) decayPerSec *= Config.LowHpHungerDecayMultiplier;
            if (enableMediumHpDecayPenalty && IsMediumLowHp) decayPerSec *= Config.MediumHpDecayMultiplier;
            SetHunger(Hunger - decayPerSec * dt);
        }

        private void TickThirst(float dt)
        {
            if (Time.time < _thirstPauseUntil) return;
            float decayPerSec = Config.ThirstDecayPerSec;
            if (IsWeakened) decayPerSec *= Config.LowHpThirstDecayMultiplier;
            if (enableMediumHpDecayPenalty && IsMediumLowHp) decayPerSec *= Config.MediumHpDecayMultiplier;
            SetThirst(Thirst - decayPerSec * dt);
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
                Heal(Config.HPRegenAmount * GetHpRegenMultiplier());
                _hpRegenTimer -= Config.HPRegenInterval;
            }
        }

        /// <summary>
        /// Hồi máu tự nhiên phụ thuộc Hunger/Thirst:
        /// - Hunger >= ngưỡng và Thirst >= ngưỡng: hồi bình thường (x1).
        /// - Chỉ một trong hai dưới ngưỡng: giảm 25% (x0.75).
        /// - Cả hai đều dưới ngưỡng: giảm 75% (x0.25).
        /// </summary>
        private float GetHpRegenMultiplier()
        {
            bool hungerOk = MaxHunger > 0f && Hunger >= Config.HungerRegenThreshold * MaxHunger;
            bool thirstOk = MaxThirst > 0f && Thirst >= Config.ThirstRegenThreshold * MaxThirst;

            if (hungerOk && thirstOk) return 1f;
            if (!hungerOk && !thirstOk) return Config.RegenMultiplierBothLow;
            return Config.RegenMultiplierSingleLow;
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