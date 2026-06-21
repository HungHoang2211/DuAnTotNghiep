using System;
using UnityEngine;
using UnityEngine.AI;

namespace SimpleSurvival.Targets
{
    public abstract class TargetableBase : MonoBehaviour, ITargetable
    {
        [Header("Colliders")]
        [SerializeField] protected SphereCollider useCollider;
        [SerializeField] protected Collider distanceCollider;
        [SerializeField] protected NavMeshObstacle navObstacle;

        private bool _destroyedFired = false;

        public virtual Transform Transform => transform;
        public float Radius => useCollider != null ? useCollider.radius : 0.5f;
        public Collider DistanceCollider => distanceCollider;
        public NavMeshObstacle NavObstacle => navObstacle;
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
    }
}