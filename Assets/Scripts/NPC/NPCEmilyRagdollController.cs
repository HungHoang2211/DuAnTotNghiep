using UnityEngine;

namespace SimpleSurvival.AI
{
    public sealed class NPCEmilyRagdollController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private NPCEmilyStats stats;
        [SerializeField] private Animator animator;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Collider mainCollider;

        [Header("Ragdoll")]
        [SerializeField] private Rigidbody[] ragdollBodies;
        [SerializeField] private Collider[] ragdollColliders;

        private void Awake()
        {
            if (stats == null) stats = GetComponent<NPCEmilyStats>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (characterController == null) characterController = GetComponent<CharacterController>();

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
            if (characterController != null) characterController.enabled = false;
            if (mainCollider != null) mainCollider.enabled = false;
            if (animator != null) animator.enabled = false;

            SetRagdollActive(true);
        }

        public void ResetRagdoll()
        {
            SetRagdollActive(false);

            if (animator != null) animator.enabled = true;
            if (mainCollider != null) mainCollider.enabled = true;
            if (characterController != null) characterController.enabled = true;
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