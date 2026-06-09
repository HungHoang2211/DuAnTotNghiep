using UnityEngine;

namespace SimpleSurvival.Combat
{
    public interface IDamageable
    {
        bool IsAlive { get; }
        bool TakeDamage(float amount, GameObject source);
    }
}