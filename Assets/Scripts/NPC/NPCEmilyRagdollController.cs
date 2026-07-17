using UnityEngine;
using UnityEngine.AI;

namespace SimpleSurvival.AI
{
    public sealed class NPCEmilyRagdollController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private NPCEmilyStats stats;
        [SerializeField] private Animator animator;
        [SerializeField] private CharacterController characterController;
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
            if (characterController == null) characterController = GetComponent<CharacterController>();
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
            // Tắt hẳn component di chuyển - đảm bảo xác không còn bị kéo đi nữa dù bất kỳ
            // logic nào khác (script khác, thứ tự gọi event...) có cố di chuyển Emily đi nữa.
            if (movement != null)
            {
                movement.Stop();
                movement.enabled = false;
            }
            if (navMeshAgent != null) navMeshAgent.enabled = false;

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