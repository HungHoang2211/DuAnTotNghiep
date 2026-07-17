using SimpleSurvival.Stats;

namespace SimpleSurvival.AI
{
    /// <summary>
    /// Stats của Emily dùng chung BaseStats (y hệt EnemyStats/PlayerStats) thay vì hệ máu tự chế
    /// riêng như trước - nhờ vậy enemy tấn công Emily sẽ áp damage đúng chuẩn (TakeDamage/IDamageable),
    /// tự bắn OnDamagedBy để trigger animation phản đòn, và tự hiện số damage bay lên qua HpHud
    /// giống hệt zombie/wolf.
    ///
    /// Cần gán 1 Base Config (BaseStatsConfig) ở Inspector - có thể tạo mới 1 asset
    /// "Simple Survival/Stats/Enemy Config" riêng cho Emily, chỉ cần điền Start HP/Max HP/Armor/
    /// MoveSpeed/BaseDamage/BaseAttackSpeed, các field AI khác (VisionRange, ChaseRadius...) bỏ qua
    /// vì Emily không dùng AI của EnemyStats.
    /// </summary>
    public sealed class NPCEmilyStats : BaseStats
    {
        // Không cần override gì thêm - mặc định HudDamageType = HpHudType.Damage
        // (giống kiểu hiển thị khi Player bị đánh, phù hợp vì Emily là NPC phe mình, không phải enemy).
    }
}