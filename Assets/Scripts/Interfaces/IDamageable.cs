namespace Xyla.Combat
{
    /// <summary>
    /// Interface chung cho mọi thứ có thể nhận damage.
    /// Enemy, Boss, Destructible object đều implement cái này.
    /// PlayerCombat chỉ cần gọi TakeDamage() mà không cần biết bên trong làm gì.
    /// </summary>
    public interface IDamageable
    {
        bool TakeDamage(float amount, UnityEngine.GameObject source);
        float CurrentHealth { get; }

        bool IsDead { get; }
    }
}