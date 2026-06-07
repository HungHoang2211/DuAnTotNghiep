using System;
using UnityEngine;

namespace SimpleSurvival.Targets
{
    public abstract class TargetableBase : MonoBehaviour, ITargetable
    {
        [SerializeField] protected float radius = 0.5f;

        public Transform Transform => transform;
        public float Radius => radius;
        public abstract TargetType Type { get; }

        public event Action<ITargetable> OnDestroyed;

        public virtual bool CanBeTargeted() => isActiveAndEnabled;

        protected virtual void OnDestroy()
        {
            OnDestroyed?.Invoke(this);
        }
    }
}