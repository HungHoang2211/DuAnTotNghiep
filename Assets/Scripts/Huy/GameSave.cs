using System;
using System.Collections.Generic;

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
        public List<CorpseData> corpses = new List<CorpseData>();
        public List<ContainerData> containers = new List<ContainerData>();
        public WorldData world = new WorldData();
        public BaseMapSaveData baseMap = new BaseMapSaveData();
        public LevelData level = new LevelData();
    }
}