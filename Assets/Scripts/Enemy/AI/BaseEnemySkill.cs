using UnityEngine;

namespace SimpleSurvival.AI
{
    public abstract class BaseEnemySkill : MonoBehaviour, IEnemySkill
    {
        [Header("Common Skill Settings")]
        [SerializeField] protected float cooldown = 1.5f;
        [SerializeField] protected float minRange = 0f;
        [SerializeField] protected float maxRange = 2f;
        [SerializeField] protected float priority = 1f;

        protected float _lastExecuteTime = -999f;
        protected bool _isExecuting;

        public bool IsExecuting => _isExecuting;
        public float Priority => priority;
        public float Cooldown => cooldown;
        public float MinRange => minRange;
        public float MaxRange => maxRange;

        public virtual bool IsAvailable(Transform target, float distanceToTarget)
        {
            if (_isExecuting) return false;
            if (target == null) return false;
            if (Time.time < _lastExecuteTime + cooldown) return false;
            if (distanceToTarget < minRange || distanceToTarget > maxRange) return false;
            return true;
        }

        public void Execute(Transform target)
        {
            if (!IsAvailable(target, Vector3.Distance(transform.position, target.position)))
                return;

            _lastExecuteTime = Time.time;
            _isExecuting = true;
            OnExecute(target);
        }

        public virtual void Cancel()
        {
            _isExecuting = false;
            OnCancel();
        }

        protected abstract void OnExecute(Transform target);
        protected virtual void OnCancel() { }

        protected void MarkComplete()
        {
            _isExecuting = false;
        }
    }
}