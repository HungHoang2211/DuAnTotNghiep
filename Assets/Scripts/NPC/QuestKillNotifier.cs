using UnityEngine;
using SimpleSurvival.Stats;
using SimpleSurvival.Quests;

namespace SimpleSurvival.AI
{
    [RequireComponent(typeof(EnemyStats))]
    public sealed class QuestKillNotifier : MonoBehaviour
    {
        private EnemyStats _stats;

        private void Awake()
        {
            _stats = GetComponent<EnemyStats>();
        }

        private void OnEnable()
        {
            if (_stats != null) _stats.OnDeath += HandleDeath;
        }

        private void OnDisable()
        {
            if (_stats != null) _stats.OnDeath -= HandleDeath;
        }

        private void HandleDeath(GameObject source)
        {
            QuestManager manager = QuestManager.Instance;
            if (manager != null && _stats != null)
                manager.NotifyEnemyKilled(_stats.EnemyConfig);
        }
    }
}