using UnityEngine;
using SimpleSurvival.Quests;
using SimpleSurvival.SaveLoad;

namespace SimpleSurvival.World
{
    public sealed class OneTimeMapLockTrigger : MonoBehaviour
    {
        [SerializeField] private string mapId;

        private void Awake()
        {
            if (string.IsNullOrEmpty(mapId) && MapLoader.Instance != null)
                mapId = MapLoader.Instance.CurrentMapScene;
        }

        private void OnDestroy()
        {
            if (!string.IsNullOrEmpty(mapId))
            {
                QuestManager.Instance?.LockMapPermanently(mapId);
                SaveService.Instance?.Save();
            }
        }
    }
}