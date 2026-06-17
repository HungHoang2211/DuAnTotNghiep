using System;
using UnityEngine;
using UnityEngine.AI;

namespace SimpleSurvival.Targets
{
    public interface ITargetable
    {
        Transform Transform { get; }
        float Radius { get; }
        Collider DistanceCollider { get; }
        NavMeshObstacle NavObstacle { get; }
        TargetType Type { get; }
        bool CanBeTargeted();
        event Action<ITargetable> OnDestroyed;
    }
}