using UnityEngine;

namespace SimpleSurvival.Combat
{
    public interface IDamageable
    {
        bool IsDead { get; }
        bool TakeDamage(float amount, GameObject source);
    }
}