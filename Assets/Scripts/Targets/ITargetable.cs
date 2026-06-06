using System;
using UnityEngine;

namespace SimpleSurvival.Targets
{
    public interface ITargetable
    {
        Transform Transform { get; }
        float Radius { get; }
        TargetType Type { get; }
        bool CanBeTargeted();
        event Action<ITargetable> OnDestroyed;
    }
}