using UnityEngine;
using SimpleSurvival.Loot;
using SimpleSurvival.Targets;

namespace SimpleSurvival.AI
{
    public sealed class EnemyCorpseHandler : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private LootContainer lootContainer;
        [SerializeField] private EnemyTarget enemyTarget;
        [SerializeField] private GameObject useTargetRoot;

        public void SpawnCorpseLoot(LootTable lootTable)
        {
            if (enemyTarget != null) enemyTarget.gameObject.SetActive(false);

            if (lootTable == null) return;

            if (lootContainer == null)
            {
                Debug.LogWarning($"[{name}] LootContainer not assigned in EnemyCorpseHandler", this);
                return;
            }

            lootContainer.InitializeRuntime(lootTable, 0f);

            if (useTargetRoot != null) useTargetRoot.SetActive(true);
        }
    }
}