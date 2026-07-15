using UnityEngine;
using System;
using SimpleSurvival.Combat;
using SimpleSurvival.UI.Hud;

namespace SimpleSurvival.Stats
{
    public abstract class BaseStats : MonoBehaviour, IDamageable
    {
        public event Action<float, float> OnHPChanged;
        public event Action<GameObject> OnDeath;
        public event Action<GameObject> OnDamagedBy;

        [SerializeField] protected BaseStatsConfig baseConfig;

        private const float ArmorK = 0.06f;
        private float _armor;
        private float _moveSpeed;
        private int _lastDamageFrame = -1;

        public float HP { get; private set; }
        public float MaxHP => baseConfig != null ? baseConfig.MaxHP : 0f;
        public float BaseDamage => baseConfig != null ? baseConfig.BaseDamage : 0f;
        public float BaseAttackSpeed => baseConfig != null ? baseConfig.BaseAttackSpeed : 0f;
        public virtual float Armor => _armor;
        public float MoveSpeed => _moveSpeed;
        public bool IsAlive { get; private set; }
        public bool IsDead => !IsAlive;
        public bool IsInvulnerable { get; private set; }

        protected virtual HpHudType HudDamageType => HpHudType.Damage;

        protected virtual void Awake()
        {
            if (baseConfig == null)
            {
                Debug.LogError($"[{name}] BaseStats config is null. Assign in Inspector.", this);
                return;
            }
            ResetStats();
        }

        public virtual void ResetStats()
        {
            if (baseConfig == null) return;
            HP = Mathf.Clamp(baseConfig.StartHP, 0f, baseConfig.MaxHP);
            _armor = baseConfig.Armor;
            _moveSpeed = baseConfig.MoveSpeed;
            IsAlive = HP > 0f;
            _lastDamageFrame = -1;
            OnHPChanged?.Invoke(HP, MaxHP);
        }

        public virtual void RestoreHP(float hp)
        {
            if (baseConfig == null) return;
            HP = Mathf.Clamp(hp, 0f, MaxHP);
            IsAlive = HP > 0f;
            _lastDamageFrame = -1;
            OnHPChanged?.Invoke(HP, MaxHP);
        }

        public bool TakeDamage(float rawDamage)
        {
            return TakeDamage(rawDamage, null);
        }

        public virtual bool TakeDamage(float rawDamage, GameObject source)
        {
            if (!IsAlive || rawDamage <= 0f)
                return IsAlive;

            if (IsInvulnerable)
                return IsAlive;

            if (source != null)
            {
                if (Time.frameCount == _lastDamageFrame)
                    return IsAlive;
                _lastDamageFrame = Time.frameCount;
            }

            float reduction = ArmorReduction(Armor);
            float finalDamage = rawDamage * (1f - reduction);
            SetHP(HP - finalDamage, source);

            Debug.Log($"[{name}] Take damage: {rawDamage} (reduced to {finalDamage:F1}) from {(source != null ? source.name : "unknown")}, HP after: {HP}");

            SpawnHpHud(finalDamage, HudDamageType);

            OnPostDamage(rawDamage, source);

            if (IsAlive && source != null)
                OnDamagedBy?.Invoke(source);

            return IsAlive;
        }

        protected virtual void OnPostDamage(float rawDamage, GameObject source) { }

        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0f)
                return;

            float prev = HP;
            SetHP(HP + amount);
            float actual = HP - prev;
            if (actual <= 0f) return;

            SpawnHpHud(actual, HpHudType.Heal);
        }

        public void SetArmor(float value)
        {
            _armor = Mathf.Max(0f, value);
        }

        public void SetMoveSpeed(float value)
        {
            _moveSpeed = Mathf.Max(0f, value);
        }

        public void SetInvulnerable(bool value)
        {
            IsInvulnerable = value;
        }

        public static float ArmorReduction(float armorValue)
        {
            float kA = ArmorK * armorValue;
            return kA / (1f + kA);
        }

        private void SpawnHpHud(float amount, HpHudType type)
        {
            HudManager hud = HudManager.Instance;
            if (hud == null || hud.HpHud == null) return;
            hud.HpHud.Spawn(transform, amount, type);
        }

        private void SetHP(float value, GameObject source = null)
        {
            float prev = HP;
            HP = Mathf.Clamp(value, 0f, MaxHP);
            if (!Mathf.Approximately(HP, prev))
                OnHPChanged?.Invoke(HP, MaxHP);

            if (HP <= 0f && IsAlive)
                Die(source);
        }

        private void Die(GameObject source)
        {
            IsAlive = false;
            OnDeath?.Invoke(source);
        }
    }
}