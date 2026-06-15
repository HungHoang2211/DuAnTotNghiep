using System;
using UnityEngine;

namespace SimpleSurvival.Targets
{
    public abstract class TargetableBase : MonoBehaviour, ITargetable
    {
        [SerializeField] protected float radius = 0.5f;

        private bool _destroyedFired = false;

        public Transform Transform => transform;
        public float Radius => radius;
        public abstract TargetType Type { get; }
        public event Action<ITargetable> OnDestroyed;

        public virtual bool CanBeTargeted() => isActiveAndEnabled;

        protected virtual void OnDestroy()
        {
            FireOnDestroyed();
        }

        protected void FireOnDestroyed()
        {
            if (_destroyedFired) return;
            _destroyedFired = true;
            OnDestroyed?.Invoke(this);
        }

        protected virtual void OnSpawnFromPool()
        {
            _destroyedFired = false;
        }
    }
}