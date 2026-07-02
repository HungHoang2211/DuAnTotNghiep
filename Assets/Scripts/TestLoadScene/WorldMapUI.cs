using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleSurvival.World
{
    public class WorldMapUI : MonoBehaviour
    {
        public static WorldMapUI Instance { get; private set; }

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Transform entryContainer;
        [SerializeField] private WorldMapEntryButton entryPrefab;
        [SerializeField] private Button closeButton;
        [SerializeField] private List<MapDestination> destinations = new List<MapDestination>();

        private readonly List<WorldMapEntryButton> spawnedEntries = new List<WorldMapEntryButton>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            panelRoot.SetActive(false);
        }

        public void Open()
        {
            if (MapTransitionController.Instance != null && MapTransitionController.Instance.IsTransitioning)
                return;

            BuildEntries();
            panelRoot.SetActive(true);
        }

        public void Close()
        {
            panelRoot.SetActive(false);
        }

        private void BuildEntries()
        {
            ClearEntries();

            string currentScene = MapLoader.Instance != null ? MapLoader.Instance.CurrentMapScene : null;

            foreach (MapDestination destination in destinations)
            {
                WorldMapEntryButton entry = Instantiate(entryPrefab, entryContainer);
                bool isCurrent = destination.SceneName == currentScene;
                entry.Bind(destination, isCurrent, HandleDestinationSelected);
                spawnedEntries.Add(entry);
            }
        }

        private void ClearEntries()
        {
            foreach (WorldMapEntryButton entry in spawnedEntries)
            {
                if (entry != null)
                    Destroy(entry.gameObject);
            }

            spawnedEntries.Clear();
        }

        private void HandleDestinationSelected(MapDestination destination)
        {
            Close();

            if (MapTransitionController.Instance != null)
                MapTransitionController.Instance.GoToMap(destination.SceneName);
        }
    }
}