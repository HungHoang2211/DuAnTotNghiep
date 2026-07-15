using UnityEngine;
using SimpleSurvival.World;

namespace SimpleSurvival.Player
{
    public sealed class PlayerCorpseMapBinding : MonoBehaviour
    {
        private string _homeMapScene;

        public void Initialize(string homeMapScene)
        {
            _homeMapScene = homeMapScene;
            DontDestroyOnLoad(gameObject);

            if (MapLoader.Instance != null)
                MapLoader.Instance.PlayerRepositioned += HandlePlayerRepositioned;

            ApplyVisibility();
        }

        private void OnDestroy()
        {
            if (MapLoader.Instance != null)
                MapLoader.Instance.PlayerRepositioned -= HandlePlayerRepositioned;
        }

        private void HandlePlayerRepositioned()
        {
            ApplyVisibility();
        }

        private void ApplyVisibility()
        {
            bool active = MapLoader.Instance != null && MapLoader.Instance.CurrentMapScene == _homeMapScene;
            gameObject.SetActive(active);
        }
    }
}