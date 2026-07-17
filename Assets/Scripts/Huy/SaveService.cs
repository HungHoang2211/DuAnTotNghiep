using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.SaveLoad
{
    public sealed class SaveService : MonoBehaviour
    {
        public static SaveService Instance { get; private set; }

        [SerializeField] private PlayerSaveAgent playerAgent;

        private SaveStorage storage;
        private float playtimeSeconds;
        private GameSave lastLoaded;

        public GameMode Mode { get; set; } = GameMode.Normal;
        public string CurrentMapId { get; set; }
        public bool IsActive { get; set; }

        public bool HasSave => storage.Exists();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            storage = new SaveStorage();
        }

        private void Update()
        {
            if (IsActive)
                playtimeSeconds += Time.deltaTime;
        }

        public bool Save()
        {
            if (!IsActive)
                return false;

            GameSave save = new GameSave
            {
                meta = new GameMeta
                {
                    mode = Mode,
                    totalPlaytimeSeconds = playtimeSeconds,
                    currentMapId = CurrentMapId
                },
                player = playerAgent.Capture(),
                corpses = CorpseSaveRegistry.Instance != null
                    ? CorpseSaveRegistry.Instance.Capture()
                    : new List<CorpseData>(),
                containers = ContainerSaveRegistry.Instance != null
                    ? ContainerSaveRegistry.Instance.Capture()
                    : new List<ContainerData>()
            };

            lastLoaded = save;
            return storage.Write(save);
        }

        public GameSave Read()
        {
            lastLoaded = storage.Read();
            return lastLoaded;
        }

        public void Apply(GameSave save)
        {
            if (save == null)
                return;

            ApplyMeta(save.meta);
            playerAgent.Restore(save.player);
        }

        public GameSave Load()
        {
            GameSave save = Read();
            Apply(save);
            return save;
        }

        public void ApplyColdBoot()
        {
            if (lastLoaded == null)
            {
                IsActive = true;
                return;
            }

            ApplyMeta(lastLoaded.meta);
            playerAgent.RestoreCrossScene(lastLoaded.player);
            IsActive = true;
        }

        public void LoadColdBoot()
        {
            Read();
            ApplyColdBoot();
        }

        public List<CorpseData> GetCorpsesForMap(string mapId)
        {
            if (lastLoaded?.corpses == null)
                return new List<CorpseData>();

            return lastLoaded.corpses.FindAll(c => c.mapId == mapId);
        }

        public ContainerData GetContainerData(string containerId)
        {
            if (lastLoaded?.containers == null)
                return null;

            return lastLoaded.containers.Find(c => c.containerId == containerId);
        }

        public void DeleteSave()
        {
            storage.Delete();
        }

        private void ApplyMeta(GameMeta meta)
        {
            if (meta == null)
                return;

            Mode = meta.mode;
            CurrentMapId = meta.currentMapId;
            playtimeSeconds = meta.totalPlaytimeSeconds;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && IsActive)
                Save();
        }

        private void OnApplicationQuit()
        {
            if (IsActive)
                Save();
        }
    }
}