using UnityEngine;

namespace SimpleSurvival.AI
{
    public abstract class BaseEnemyAnimator : MonoBehaviour
    {
        [SerializeField] protected Animator _animator;

        [Header("Ragdoll")]
        [SerializeField] protected Rigidbody[] _ragdollBodies;
        [SerializeField] protected Collider[] _ragdollColliders;

        protected virtual void Awake()
        {
            if (_animator == null) _animator = GetComponent<Animator>();
            SetRagdollActive(false);
        }

        public abstract void SetMoving(bool moving);
        public abstract void SetIdle();
        public abstract void TriggerAttack(int attackIndex);
        public abstract void TriggerDeath();
        public abstract void ResetForSpawn();

        public virtual void CancelAttack() { }

        public void SetRagdollActive(bool active)
        {
            if (_ragdollBodies != null)
            {
                foreach (var rb in _ragdollBodies)
                {
                    if (rb != null) rb.isKinematic = !active;
                }
            }

            if (_ragdollColliders != null)
            {
                foreach (var col in _ragdollColliders)
                {
                    if (col != null) col.enabled = active;
                }
            }
        }

        public void SetRagdollLayer(int layer)
        {
            if (layer < 0 || _ragdollColliders == null) return;
            foreach (var col in _ragdollColliders)
            {
                if (col != null) col.gameObject.layer = layer;
            }
        }
    }
}