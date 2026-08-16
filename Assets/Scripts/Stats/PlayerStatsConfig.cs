using UnityEngine;

namespace SimpleSurvival.Stats
{
    [CreateAssetMenu(menuName = "Simple Survival/Stats/Player Stats Config", fileName = "PlayerStatsConfig")]
    public sealed class PlayerStatsConfig : BaseStatsConfig
    {
        [Header("Hunger")]
        [SerializeField] private float maxHunger = 100f;
        [SerializeField] private float startHunger = 100f;
        [SerializeField] private float hungerDecayPerSec = 0.01f;

        [Header("Thirst")]
        [SerializeField] private float maxThirst = 100f;
        [SerializeField] private float startThirst = 100f;
        [SerializeField] private float thirstDecayPerSec = 0.03f;

        [Header("HP Regen (Tick-based)")]
        [Tooltip("HP healed per tick.")]
        [SerializeField] private float hpRegenAmount = 5f;
        [Tooltip("Seconds between each regen tick.")]
        [SerializeField] private float hpRegenInterval = 1f;

        [Header("Starvation Damage (Tick-based)")]
        [SerializeField] private float starveDamageAmount = 5f;
        [SerializeField] private float starveDamageInterval = 1f;

        [Header("Dehydrate Damage (Tick-based)")]
        [SerializeField] private float dehydrateDamageAmount = 5f;
        [SerializeField] private float dehydrateDamageInterval = 1f;

        [Header("Movement")]
        [Tooltip("Hệ số nhân với weight của vũ khí để tính phần trăm giảm tốc. " +
            "Ví dụ 0.02 = mỗi 1 weight giảm 2% tốc độ chạy.")]
        [SerializeField] private float weightSpeedFactor = 0.02f;

        [Header("Low HP / Hunger / Thirst Penalty")]
        [Tooltip("Ngưỡng % HP (so với MaxHP) mà dưới đó bắt đầu áp dụng các penalty. Mặc định 0.5 = dưới 50% HP.")]
        [SerializeField] private float lowHpThreshold = 0.5f;

        [Tooltip("Ngưỡng % Hunger (so với MaxHunger) mà bằng/dưới đó cũng áp dụng chung penalty như Low HP. " +
            "Mặc định 0.3 = 30% (ví dụ Hunger <= 30 khi MaxHunger = 100).")]
        [SerializeField] private float lowHungerThreshold = 0.3f;

        [Tooltip("Ngưỡng % Thirst (so với MaxThirst) mà bằng/dưới đó cũng áp dụng chung penalty như Low HP. " +
            "Mặc định 0.3 = 30% (ví dụ Thirst <= 30 khi MaxThirst = 100).")]
        [SerializeField] private float lowThirstThreshold = 0.3f;

        [Tooltip("Hệ số nhân damage khi HP thấp, áp dụng cho vũ khí cận chiến (Melee) và base damage (unarmed). " +
            "0.7 = giảm 30% damage.")]
        [SerializeField] private float lowHpMeleeDamageMultiplier = 0.7f;

        [Tooltip("Hệ số nhân Range của vũ khí tầm xa (Pistol/Rifle) khi HP thấp — dùng thay cho hitrate vì WeaponAbility " +
            "hiện chưa có stat hitrate riêng. 0.2 = giảm 80% range.")]
        [SerializeField] private float lowHpRangedRangeMultiplier = 0.2f;

        [Tooltip("Hệ số nhân AttackSpeed (tốc độ bắn) của vũ khí tầm xa (Pistol/Rifle) khi HP thấp. 0.5 = giảm 50% tốc độ bắn.")]
        [SerializeField] private float lowHpRangedAttackSpeedMultiplier = 0.5f;

        [Tooltip("Hệ số nhân MoveSpeed khi HP thấp. 0.8 = giảm 20% tốc độ di chuyển.")]
        [SerializeField] private float lowHpMoveSpeedMultiplier = 0.8f;

        [Tooltip("Hệ số nhân tốc độ giảm Hunger khi HP thấp. 1.2 = Hunger giảm nhanh hơn 20%.")]
        [SerializeField] private float lowHpHungerDecayMultiplier = 1.2f;

        [Tooltip("Hệ số nhân tốc độ giảm Thirst khi HP thấp. 1.4 = Thirst giảm nhanh hơn 40%.")]
        [SerializeField] private float lowHpThirstDecayMultiplier = 1.4f;

        [Header("Medium HP Decay Penalty (cộng dồn thêm, riêng cho Hunger/Thirst)")]
        [Tooltip("Ngưỡng % HP (so với MaxHP) mà dưới đó Hunger/Thirst giảm nhanh gấp đôi — độc lập với LowHpThreshold ở trên " +
            "(75% cao hơn 50% nên kích hoạt sớm hơn, chỉ ảnh hưởng tốc độ giảm Hunger/Thirst, không ảnh hưởng damage/range/movespeed). " +
            "Mặc định 0.75 = dưới 75% HP.")]
        [SerializeField] private float mediumHpThreshold = 0.75f;

        [Tooltip("Hệ số nhân thêm vào tốc độ giảm Hunger/Thirst khi HP dưới MediumHpThreshold. 2 = giảm nhanh gấp đôi. " +
            "Cộng dồn (nhân) với LowHpHungerDecayMultiplier/LowHpThirstDecayMultiplier nếu HP đồng thời dưới cả 2 ngưỡng.")]
        [SerializeField] private float mediumHpDecayMultiplier = 2f;

        [Header("Teleport Cost")]
        [Tooltip("Hunger bị trừ mỗi lần player teleport (đổi map qua MapTransitionController.GoToMap). " +
            "Không thể làm Hunger xuống dưới 0.")]
        [SerializeField] private float teleportHungerCost = 20f;

        [Tooltip("Thirst bị trừ mỗi lần player teleport. Không thể làm Thirst xuống dưới 0.")]
        [SerializeField] private float teleportThirstCost = 30f;

        [Header("HP Regen theo Hunger/Thirst")]
        [Tooltip("Ngưỡng % Hunger (so với MaxHunger) để HP hồi bình thường. Mặc định 0.7 = 70%.")]
        [SerializeField] private float hungerRegenThreshold = 0.7f;

        [Tooltip("Ngưỡng % Thirst (so với MaxThirst) để HP hồi bình thường. Mặc định 0.5 = 50%.")]
        [SerializeField] private float thirstRegenThreshold = 0.5f;

        [Tooltip("Hệ số hồi máu khi CHỈ MỘT trong hai chỉ số (Hunger HOẶC Thirst) dưới ngưỡng. 0.75 = giảm 25%.")]
        [SerializeField] private float regenMultiplierSingleLow = 0.75f;

        [Tooltip("Hệ số hồi máu khi CẢ HAI Hunger và Thirst đều dưới ngưỡng. 0.25 = giảm 75%.")]
        [SerializeField] private float regenMultiplierBothLow = 0.25f;

        public float MaxHunger => maxHunger;
        public float StartHunger => startHunger;
        public float HungerDecayPerSec => hungerDecayPerSec;

        public float MaxThirst => maxThirst;
        public float StartThirst => startThirst;
        public float ThirstDecayPerSec => thirstDecayPerSec;

        public float HPRegenAmount => hpRegenAmount;
        public float HPRegenInterval => hpRegenInterval;

        public float StarveDamageAmount => starveDamageAmount;
        public float StarveDamageInterval => starveDamageInterval;

        public float DehydrateDamageAmount => dehydrateDamageAmount;
        public float DehydrateDamageInterval => dehydrateDamageInterval;

        public float WeightSpeedFactor => weightSpeedFactor;

        public float LowHpThreshold => lowHpThreshold;
        public float LowHungerThreshold => lowHungerThreshold;
        public float LowThirstThreshold => lowThirstThreshold;
        public float LowHpMeleeDamageMultiplier => lowHpMeleeDamageMultiplier;
        public float LowHpRangedRangeMultiplier => lowHpRangedRangeMultiplier;
        public float LowHpRangedAttackSpeedMultiplier => lowHpRangedAttackSpeedMultiplier;
        public float LowHpMoveSpeedMultiplier => lowHpMoveSpeedMultiplier;
        public float LowHpHungerDecayMultiplier => lowHpHungerDecayMultiplier;
        public float LowHpThirstDecayMultiplier => lowHpThirstDecayMultiplier;

        public float MediumHpThreshold => mediumHpThreshold;
        public float MediumHpDecayMultiplier => mediumHpDecayMultiplier;

        public float TeleportHungerCost => teleportHungerCost;
        public float TeleportThirstCost => teleportThirstCost;

        public float HungerRegenThreshold => hungerRegenThreshold;
        public float ThirstRegenThreshold => thirstRegenThreshold;
        public float RegenMultiplierSingleLow => regenMultiplierSingleLow;
        public float RegenMultiplierBothLow => regenMultiplierBothLow;
    }
}