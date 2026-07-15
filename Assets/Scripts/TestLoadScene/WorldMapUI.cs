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

        private bool isPaused;
        private bool waitingForTransition;

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

        private void OnDisable()
        {
            // Lưới an toàn: nếu object bị disable lúc đang pause (đổi scene, tắt UI...) thì trả timeScale lại
            Resume();
        }

        public void Open()
        {
            if (MapTransitionController.Instance != null && MapTransitionController.Instance.IsTransitioning)
                return;

            BuildEntries();
            panelRoot.SetActive(true);
            Pause();
        }

        public void Close()
        {
            panelRoot.SetActive(false);

            // Nếu vừa chọn 1 điểm đến, giữ pause tới khi MapTransitionController báo xong,
            // tránh player/enemy chạy trong lúc fade + load map.
            if (!waitingForTransition)
                Resume();
        }

        private void Pause()
        {
            if (isPaused) return;
            Time.timeScale = 0f;
            isPaused = true;
        }

        private void Resume()
        {
            if (!isPaused) return;
            Time.timeScale = 1f;
            isPaused = false;
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

                RectTransform entryRect = (RectTransform)entry.transform;
                entryRect.anchoredPosition = destination.MapPosition;

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
            if (MapTransitionController.Instance == null)
            {
                Close();
                return;
            }

            waitingForTransition = true;
            MapTransitionController.Instance.TransitionFinished += HandleTransitionFinished;

            Close();
            MapTransitionController.Instance.GoToMap(destination.SceneName);
        }

        private void HandleTransitionFinished()
        {
            MapTransitionController.Instance.TransitionFinished -= HandleTransitionFinished;
            waitingForTransition = false;
            Resume();
        }
    }
}