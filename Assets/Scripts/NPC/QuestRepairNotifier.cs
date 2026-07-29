using UnityEngine;
using SimpleSurvival.Targets;

namespace SimpleSurvival.Quests
{
    [RequireComponent(typeof(RepairableTower))]
    public sealed class QuestRepairNotifier : MonoBehaviour
    {
        private RepairableTower _tower;

        private void Awake()
        {
            _tower = GetComponent<RepairableTower>();
        }

        private void OnEnable()
        {
            if (_tower != null) _tower.OnRepaired += HandleRepaired;
        }

        private void OnDisable()
        {
            if (_tower != null) _tower.OnRepaired -= HandleRepaired;
        }

        private void HandleRepaired(RepairableTower tower)
        {
            QuestManager manager = QuestManager.Instance;
            if (manager != null)
                manager.NotifyTowerRepaired(tower.TowerId);
        }
    }
}