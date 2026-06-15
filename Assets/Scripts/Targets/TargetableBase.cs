using System;
using UnityEngine;
using UnityEngine.AI;

namespace SimpleSurvival.Targets
{
    public abstract class TargetableBase : MonoBehaviour, ITargetable
    {
        [Header("Colliders")]
        [Tooltip("SphereCollider isTrigger=true. Radius xác định size vòng marker dưới chân target.")]
        [SerializeField] protected SphereCollider useCollider;

        [Tooltip("Collider isTrigger=false. Vừa block player vừa dùng cho ClosestPoint (attack range/distance check).")]
        [SerializeField] protected Collider distanceCollider;

        [Tooltip("Optional. NavMeshObstacle để enemy AI tránh pathfind through target.")]
        [SerializeField] protected NavMeshObstacle navObstacle;

        private bool _destroyedFired = false;

        public Transform Transform => transform;
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

        protected virtual void OnSpawnFromPool()
        {
            _destroyedFired = false;
        }
    }
}