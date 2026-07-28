using UnityEngine;
using SimpleSurvival.Stats;
using SimpleSurvival.Targets;

namespace SimpleSurvival.Quests
{
    [RequireComponent(typeof(HarvestTarget))]
    [RequireComponent(typeof(HarvestStats))]
    public sealed class QuestHarvestNotifier : MonoBehaviour
    {
        private HarvestTarget _target;
        private HarvestStats _stats;

        private void Awake()
        {
            _target = GetComponent<HarvestTarget>();
            _stats = GetComponent<HarvestStats>();
        }

        private void OnEnable()
        {
            if (_stats != null) _stats.OnDepleted += HandleDepleted;
        }

        private void OnDisable()
        {
            if (_stats != null) _stats.OnDepleted -= HandleDepleted;
        }

        private void HandleDepleted()
        {
            QuestManager manager = QuestManager.Instance;
            if (manager != null && _target != null)
                manager.NotifyHarvestCompleted(_target.ItemData);
        }
    }
}