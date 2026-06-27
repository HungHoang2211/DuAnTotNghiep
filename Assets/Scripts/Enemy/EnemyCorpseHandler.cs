using UnityEngine;
using SimpleSurvival.Loot;

namespace SimpleSurvival.AI
{
    public sealed class EnemyCorpseHandler : MonoBehaviour
    {
        [Header("Refs")]
        [Tooltip("LootContainer component trên enemy GameObject. Nên được disable từ đầu (deferInitialization = true).")]
        [SerializeField] private LootContainer lootContainer;

        [Tooltip("Collider/component để player target xác (LootTarget). Disable từ đầu.")]
        [SerializeField] private Collider lootTargetCollider;

        public void SpawnCorpseLoot(LootTable lootTable)
        {
            // Không có loot table = không có gì để loot = xác không tương tác
            if (lootTable == null)
            {
                Debug.Log($"[{name}] No corpse loot table, corpse non-interactive");
                return;
            }

            if (lootContainer == null)
            {
                Debug.LogWarning($"[{name}] LootContainer not assigned in EnemyCorpseHandler", this);
                return;
            }

            // Init container với table
            //lootContainer.InitializeRuntime(lootTable, 0f);  // unlockDuration=0 → open ngay

            // Enable loot target
            if (lootTargetCollider != null)
                lootTargetCollider.enabled = true;
        }
    }
}