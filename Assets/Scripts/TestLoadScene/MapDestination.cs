using UnityEngine;
using SimpleSurvival.Quests;

namespace SimpleSurvival.World
{
    public enum MapUnlockCondition
    {
        None,
        OnQuestAccepted,
        OnQuestCompleted
    }

    [CreateAssetMenu(menuName = "SimpleSurvival/Map Destination")]
    public class MapDestination : ScriptableObject
    {
        [SerializeField] private string sceneName;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;
        [SerializeField] private Vector2 mapPosition;

        [Header("Unlock")]
        [SerializeField] private MapUnlockCondition unlockCondition = MapUnlockCondition.None;
        [SerializeField] private QuestData unlockQuest;

        public string SceneName => sceneName;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public Vector2 MapPosition => mapPosition;
        public MapUnlockCondition UnlockCondition => unlockCondition;
        public QuestData UnlockQuest => unlockQuest;
    }
}