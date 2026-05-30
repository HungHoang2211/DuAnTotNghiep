using UnityEngine;
using System;

namespace SimpleSurvival.Stats
{
    /// <summary>
    /// Chỉ số cơ bản dùng chung cho mọi entity (player, enemy).
    /// Subclass mở rộng thêm chỉ số riêng — PlayerStats thêm Hunger/Thirst,
    /// EnemyStats thêm aggro range, drop table,...
    ///
    /// Công thức giảm damage theo Armor:
    ///   reduction = k × armor / (1 + k × armor), k = 0.06
    ///   Armor không bao giờ đạt giảm 100%.
    /// </summary>
    public abstract class BaseStats : MonoBehaviour
    {
        // ── Events ───────────────────────────────────────────────────────────

        /// <summary>Raised khi HP thay đổi. Args: currentHP, maxHP.</summary>
        public event Action<float, float> OnHPChanged;

        /// <summary>Raised một lần duy nhất khi HP về 0.</summary>
        public event Action OnDeath;

        // ── Inspector ────────────────────────────────────────────────────────

        [Header("HP")]
        [SerializeField] private float maxHP = 100f;
        [SerializeField] private float startHP = 100f;

        [Header("Combat")]
        [Tooltip("Sát thương cơ bản khi không có vũ khí (đấm tay).")]
        [SerializeField] private float baseDamage = 10f;

        [Tooltip("Tốc độ tấn công cơ bản khi không có vũ khí.")]
        [SerializeField] private float baseAttackSpeed = 1f;

        [Tooltip("Giáp — giảm damage nhận vào theo công thức lợi nhuận giảm dần.")]
        [SerializeField] private float armor = 0f;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;

        // ── Armor formula constant ────────────────────────────────────────────
        private const float ArmorK = 0.06f;

        // ── Properties ───────────────────────────────────────────────────────

        public float HP { get; private set; }
        public float MaxHP => maxHP;
        public float BaseDamage => baseDamage;
        public float BaseAttackSpeed => baseAttackSpeed;
        public float Armor => armor;
        public float MoveSpeed => moveSpeed;
        public bool IsAlive { get; private set; }

        // ── Lifecycle ────────────────────────────────────────────────────────

        protected virtual void Awake()
        {
            HP = Mathf.Clamp(startHP, 0f, maxHP);
            IsAlive = HP > 0f;
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Nhận damage thô. Armor tự động được áp dụng bên trong.
        /// Combat system truyền damage của vũ khí vào đây — không cần tính armor ở ngoài.
        /// </summary>
        public void TakeDamage(float rawDamage)
        {
            if (!IsAlive || rawDamage <= 0f)
                return;

            float reduction = ArmorReduction(armor);
            float finalDamage = rawDamage * (1f - reduction);
            SetHP(HP - finalDamage);
        }

        /// <summary>Hồi HP trực tiếp (thuốc, kỹ năng...).</summary>
        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0f)
                return;

            SetHP(HP + amount);
        }

        /// <summary>
        /// Thay đổi armor tạm thời (buff/debuff từ trang bị).
        /// Equipment system gọi khi equip/unequip.
        /// </summary>
        public void SetArmor(float value)
        {
            armor = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Thay đổi move speed tạm thời (buff/debuff từ trang bị, status effect).
        /// </summary>
        public void SetMoveSpeed(float value)
        {
            moveSpeed = Mathf.Max(0f, value);
        }

        // ── Armor formula ─────────────────────────────────────────────────────

        /// <summary>
        /// Tính tỉ lệ giảm damage [0, 1) theo công thức GDD.
        /// reduction = k × armor / (1 + k × armor)
        /// </summary>
        public static float ArmorReduction(float armorValue)
        {
            float kA = ArmorK * armorValue;
            return kA / (1f + kA);
        }

        // ── Private ──────────────────────────────────────────────────────────

        private void SetHP(float value)
        {
            float prev = HP;
            HP = Mathf.Clamp(value, 0f, maxHP);

            if (!Mathf.Approximately(HP, prev))
                OnHPChanged?.Invoke(HP, maxHP);

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