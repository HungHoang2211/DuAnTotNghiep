using System;

namespace SimpleSurvival.SaveLoad
{
    public enum GameMode
    {
        Normal = 0,
        Hard = 1
    }

    [Serializable]
    public sealed class GameMeta
    {
        public GameMode mode;
        public float totalPlaytimeSeconds;
        public string currentMapId;
    }

    [Serializable]
    public sealed class GameSave
    {
        public const int CurrentVersion = 1;

        public int saveVersion = CurrentVersion;
        public GameMeta meta = new GameMeta();
        public PlayerData player = new PlayerData();
    }
}