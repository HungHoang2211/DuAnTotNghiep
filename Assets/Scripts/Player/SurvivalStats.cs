using UnityEngine;
using System;

namespace Xyla.Player
{
    /// Chỉ số sinh tồn: HP, Hunger, Thirst.
    ///
    /// LOGIC:
    ///   - Hunger và Thirst giảm dần theo thời gian
    ///   - HP hồi chậm khi Hunger và Thirst đều đủ ngưỡng
    ///   - Mỗi tick hồi HP tiêu hao 1 lượng Hunger và Thirst
    ///   - HP giảm khi Hunger hoặc Thirst về 0 (chết đói/khát)

    /// SETUP:
    ///   Gắn lên Player. Các script khác đọc qua property hoặc đăng ký event.
    public class SurvivalStats : MonoBehaviour
    {
        // ── Events ───────────────────────────────────────────────────────────
        public event Action<float, float> OnHPChanged;      
        public event Action<float, float> OnHungerChanged;  
        public event Action<float, float> OnThirstChanged;  
        public event Action OnDeath;

        // ── Inspector ────────────────────────────────────────────────────────
        [Header("HP")]
        [SerializeField] private float _maxHP = 100f;
        [SerializeField] private float _startHP = 100f;

        [Tooltip("HP hồi mỗi giây khi đủ Hunger và Thirst.")]
        [SerializeField] private float _hpRegenPerSec = 2f;

        [Tooltip("Hunger và Thirst tối thiểu để được hồi HP (0-1, ví dụ 0.2 = 20%).")]
        [SerializeField][Range(0f, 1f)] private float _regenThreshold = 0.2f;

        [Tooltip("Hunger tiêu hao mỗi 1 HP được hồi.")]
        [SerializeField] private float _hungerCostPerHPRegen = 0.5f;

        [Tooltip("Thirst tiêu hao mỗi 1 HP được hồi.")]
        [SerializeField] private float _thirstCostPerHPRegen = 0.8f;

        [Header("Hunger")]
        [SerializeField] private float _maxHunger = 100f;
        [SerializeField] private float _startHunger = 100f;

        [Tooltip("Hunger giảm mỗi giây.")]
        [SerializeField] private float _hungerDecayPerSec = 0.01f;

        [Tooltip("HP giảm mỗi giây khi Hunger = 0.")]
        [SerializeField] private float _starveDamagePerSec = 1f;

        [Header("Thirst")]
        [SerializeField] private float _maxThirst = 100f;
        [SerializeField] private float _startThirst = 100f;

        [Tooltip("Thirst giảm mỗi giây.")]
        [SerializeField] private float _thirstDecayPerSec = 0.03f;

        [Tooltip("HP giảm mỗi giây khi Thirst = 0.")]
        [SerializeField] private float _dehydrateDamagePerSec = 1.5f;

        // ── Properties ───────────────────────────────────────────────────────
        public float HP { get; private set; }
        public float Hunger { get; private set; }
        public float Thirst { get; private set; }
        public float MaxHP => _maxHP;
        public float MaxHunger => _maxHunger;
        public float MaxThirst => _maxThirst;
        public bool IsDead { get; private set; }

        // ── Lifecycle ────────────────────────────────────────────────────────
        private void Awake()
        {
            HP = Mathf.Clamp(_startHP, 0f, _maxHP);
            Hunger = Mathf.Clamp(_startHunger, 0f, _maxHunger);
            Thirst = Mathf.Clamp(_startThirst, 0f, _maxThirst);
        }

        private void Update()
        {
            if (IsDead) return;

            float dt = Time.deltaTime;

            TickHunger(dt);
            TickThirst(dt);
            TickHPRegen(dt);
            TickStarvation(dt);
        }



        // ── Ticks ────────────────────────────────────────────────────────────

        private void TickHunger(float dt)
        {
            SetHunger(Hunger - _hungerDecayPerSec * dt);
        }

        private void TickThirst(float dt)
        {
            SetThirst(Thirst - _thirstDecayPerSec * dt);
        }

        private void TickHPRegen(float dt)
        {
            // Chỉ hồi khi cả 2 đều trên ngưỡng
            bool hungerOk = Hunger / _maxHunger >= _regenThreshold;
            bool thirstOk = Thirst / _maxThirst >= _regenThreshold;
            if (!hungerOk || !thirstOk) return;
            if (HP >= _maxHP) return;

            float regenAmount = _hpRegenPerSec * dt;

            // Tiêu hao Hunger và Thirst làm nguyên liệu
            float hungerCost = _hungerCostPerHPRegen * regenAmount;
            float thirstCost = _thirstCostPerHPRegen * regenAmount;

            // Nếu không đủ nguyên liệu thì hồi ít đi
            float ratio = Mathf.Min(
                hungerCost > 0 ? Hunger / hungerCost : 1f,
                thirstCost > 0 ? Thirst / thirstCost : 1f,
                1f);

            regenAmount *= ratio;
            hungerCost *= ratio;
            thirstCost *= ratio;

            SetHP(HP + regenAmount);
            SetHunger(Hunger - hungerCost);
            SetThirst(Thirst - thirstCost);
        }

        private void TickStarvation(float dt)
        {
            // Đói → mất máu
            if (Hunger <= 0f)
                TakeDamage(_starveDamagePerSec * dt);

            // Khát → mất máu (nhanh hơn đói)
            if (Thirst <= 0f)
                TakeDamage(_dehydrateDamagePerSec * dt);
        }


        /// Nhận damage (từ enemy, môi trường...).
        public void TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f) return;
            SetHP(HP - amount);
        }

        /// Ăn thức ăn → tăng Hunger.
        /// Script item gọi cái này với giá trị tùy loại đồ ăn.
        public void AddHunger(float amount)
        {
            if (amount <= 0f) return;
            SetHunger(Hunger + amount);
        }

        /// Uống nước → tăng Thirst.
        /// Script item gọi cái này với giá trị tùy loại nước.
        public void AddThirst(float amount)
        {
            if (amount <= 0f) return;
            SetThirst(Thirst + amount);
        }

        /// Hồi HP trực tiếp (thuốc, kỹ năng...).
        public void Heal(float amount)
        {
            if (amount <= 0f) return;
            SetHP(HP + amount);
        }
        private void SetHP(float value)
        {
            float prev = HP;
            HP = Mathf.Clamp(value, 0f, _maxHP);
            if (!Mathf.Approximately(HP, prev))
                OnHPChanged?.Invoke(HP, _maxHP);

            if (HP <= 0f && !IsDead)
                Die();
        }

        private void SetHunger(float value)
        {
            float prev = Hunger;
            Hunger = Mathf.Clamp(value, 0f, _maxHunger);
            if (!Mathf.Approximately(Hunger, prev))
                OnHungerChanged?.Invoke(Hunger, _maxHunger);
        }

        private void SetThirst(float value)
        {
            float prev = Thirst;
            Thirst = Mathf.Clamp(value, 0f, _maxThirst);
            if (!Mathf.Approximately(Thirst, prev))
                OnThirstChanged?.Invoke(Thirst, _maxThirst);
        }

        private void Die()
        {
            IsDead = true;
            OnDeath?.Invoke();
            Debug.Log($"{name} đã chết.");
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;
            // Debug bar nhỏ trong Scene view
            DrawDebugBar(Vector3.up * 2.5f, HP / _maxHP, Color.red);
            DrawDebugBar(Vector3.up * 2.8f, Hunger / _maxHunger, Color.yellow);
            DrawDebugBar(Vector3.up * 3.1f, Thirst / _maxThirst, Color.cyan);
        }

        private void DrawDebugBar(Vector3 offset, float ratio, Color color)
        {
            Gizmos.color = color;
            Gizmos.DrawCube(
                transform.position + offset + Vector3.right * (ratio - 1f),
                new Vector3(ratio * 2f, 0.08f, 0.08f));
        }
#endif
    }
}