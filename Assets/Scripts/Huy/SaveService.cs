using UnityEngine;

namespace SimpleSurvival.SaveLoad
{
    public sealed class SaveService : MonoBehaviour
    {
        [SerializeField] private PlayerSaveAgent playerAgent;

        private SaveStorage storage;
        private float playtimeSeconds;

        public GameMode Mode { get; set; } = GameMode.Normal;
        public string CurrentMapId { get; set; }
        public bool IsActive { get; set; }

        public bool HasSave => storage.Exists();

        private void Awake()
        {
            storage = new SaveStorage();
        }

        private void Update()
        {
            if (IsActive)
                playtimeSeconds += Time.deltaTime;
        }

        public bool Save()
        {
            GameSave save = new GameSave
            {
                meta = new GameMeta
                {
                    mode = Mode,
                    totalPlaytimeSeconds = playtimeSeconds,
                    currentMapId = CurrentMapId
                },
                player = playerAgent.Capture()
            };

            return storage.Write(save);
        }

        public GameSave Read()
        {
            return storage.Read();
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