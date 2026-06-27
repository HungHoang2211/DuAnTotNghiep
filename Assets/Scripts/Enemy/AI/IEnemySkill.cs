using UnityEngine;

namespace SimpleSurvival.AI
{
    public interface IEnemySkill
    {
        bool IsAvailable(Transform target, float distanceToTarget);
        void Execute(Transform target);
        void Cancel();
        bool IsExecuting { get; }
        float Priority { get; }
    }
}