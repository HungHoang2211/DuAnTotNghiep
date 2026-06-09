using UnityEngine;
using System;
using SimpleSurvival.Combat;

namespace SimpleSurvival.Stats
{
    public abstract class BaseStats : MonoBehaviour, IDamageable
    {
        public event Action<float, float> OnHPChanged;
        public event Action OnDeath;
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
        public float Armor => _armor;
        public float MoveSpeed => _moveSpeed;
        public bool IsAlive { get; private set; }
        public bool IsDead => !IsAlive;

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

        public bool TakeDamage(float rawDamage)
        {
            return TakeDamage(rawDamage, null);
        }

        public bool TakeDamage(float rawDamage, GameObject source)
        {
            if (!IsAlive || rawDamage <= 0f)
                return IsAlive;

            if (source != null)
            {
                if (Time.frameCount == _lastDamageFrame)
                    return IsAlive;
                _lastDamageFrame = Time.frameCount;
            }

            float reduction = ArmorReduction(_armor);
            float finalDamage = rawDamage * (1f - reduction);
            SetHP(HP - finalDamage);

            Debug.Log($"[{name}] Take damage: {rawDamage} from {(source != null ? source.name : "unknown")}, HP after: {HP}");

            if (IsAlive && source != null)
                OnDamagedBy?.Invoke(source);

            return IsAlive;
        }

        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0f)
                return;

            SetHP(HP + amount);
        }

        public void SetArmor(float value)
        {
            _armor = Mathf.Max(0f, value);
        }

        public void SetMoveSpeed(float value)
        {
            _moveSpeed = Mathf.Max(0f, value);
        }

        public static float ArmorReduction(float armorValue)
        {
            float kA = ArmorK * armorValue;
            return kA / (1f + kA);
        }

        private void SetHP(float value)
        {
            float prev = HP;
            HP = Mathf.Clamp(value, 0f, MaxHP);

            if (!Mathf.Approximately(HP, prev))
                OnHPChanged?.Invoke(HP, MaxHP);

            if (HP <= 0f && IsAlive)
                Die();
        }

        private void Die()
        {
            IsAlive = false;
            OnDeath?.Invoke();
        }
    }
}