using UnityEngine;

namespace SimpleSurvival.World
{
    [CreateAssetMenu(menuName = "SimpleSurvival/Map Destination")]
    public class MapDestination : ScriptableObject
    {
        [SerializeField] private string sceneName;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;
        [SerializeField] private Vector2 mapPosition;

        public string SceneName => sceneName;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public Vector2 MapPosition => mapPosition;
    }
}