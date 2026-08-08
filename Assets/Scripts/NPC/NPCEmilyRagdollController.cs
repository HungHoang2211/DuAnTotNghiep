using UnityEngine;
using UnityEngine.AI;

namespace SimpleSurvival.AI
{
    public sealed class NPCEmilyRagdollController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private NPCEmilyStats stats;
        [SerializeField] private Animator animator;
        [SerializeField] private Collider bodyCollider;
        [SerializeField] private Collider mainCollider;
        [SerializeField] private NPCEmilyMovement movement;
        [SerializeField] private NavMeshAgent navMeshAgent;

        [Header("Ragdoll")]
        [SerializeField] private Rigidbody[] ragdollBodies;
        [SerializeField] private Collider[] ragdollColliders;

        private void Awake()
        {
            if (stats == null) stats = GetComponent<NPCEmilyStats>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (bodyCollider == null) bodyCollider = GetComponent<CapsuleCollider>();
            if (movement == null) movement = GetComponent<NPCEmilyMovement>();
            if (navMeshAgent == null) navMeshAgent = GetComponent<NavMeshAgent>();

            SetRagdollActive(false);

            if (stats != null)
                stats.OnDeath += HandleDeath;
        }

        private void OnDestroy()
        {
            if (stats != null)
                stats.OnDeath -= HandleDeath;
        }

        private void HandleDeath(GameObject source)
        {
            if (movement != null)
            {
                movement.Stop();
                movement.enabled = false;
            }
            if (navMeshAgent != null) navMeshAgent.enabled = false;

            if (bodyCollider != null) bodyCollider.enabled = false;
            if (mainCollider != null) mainCollider.enabled = false;
            if (animator != null) animator.enabled = false;

            SetRagdollActive(true);
        }

        public void ResetRagdoll()
        {
            SetRagdollActive(false);

            if (animator != null) animator.enabled = true;
            if (mainCollider != null) mainCollider.enabled = true;
            if (bodyCollider != null) bodyCollider.enabled = true;
            if (navMeshAgent != null) navMeshAgent.enabled = true;
            if (movement != null) movement.enabled = true;
        }

        private void SetRagdollActive(bool active)
        {
            foreach (var rb in ragdollBodies)
                if (rb != null) rb.isKinematic = !active;

            foreach (var col in ragdollColliders)
                if (col != null) col.enabled = active;
        }
    }
}