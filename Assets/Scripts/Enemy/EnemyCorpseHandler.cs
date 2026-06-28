using UnityEngine;
using SimpleSurvival.Loot;
using SimpleSurvival.Targets;

namespace SimpleSurvival.AI
{
    public sealed class EnemyCorpseHandler : MonoBehaviour
    {
        [Header("Refs")]
        [Tooltip("LootContainer trên child UseTarget. Phải có deferInitialization = true.")]
        [SerializeField] private LootContainer lootContainer;

        [Tooltip("EnemyTarget component (ở enemy root) - sẽ disable khi chết.")]
        [SerializeField] private EnemyTarget enemyTarget;

        [Tooltip("Child GameObject chứa UseTarget (LootContainer) - disabled từ đầu, enable khi chết.")]
        [SerializeField] private GameObject useTargetRoot;

        public void SpawnCorpseLoot(LootTable lootTable)
        {
            Debug.Log($"[{name}] SpawnCorpseLoot called, lootTable={lootTable?.name}, enemyTarget={enemyTarget != null}, lootContainer={lootContainer != null}, useTargetRoot={useTargetRoot != null}");

            // Disable enemy target marker (không attack được nữa)
            if (enemyTarget != null) enemyTarget.gameObject.SetActive(false);

            // Không có loot table = corpse không tương tác
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

            // Init LootContainer với table (unlockDuration=0 → open ngay)
            lootContainer.InitializeRuntime(lootTable, 0f);
            Debug.Log($"[{name}] InitializeRuntime done, enabling useTargetRoot");

            // Enable use target root
            if (useTargetRoot != null) useTargetRoot.SetActive(true);
        }
    }
}