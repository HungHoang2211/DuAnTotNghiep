using System;

namespace SimpleSurvival.SaveLoad
{
    [Serializable]
    public sealed class LevelData
    {
        public int level = 1;
        public int currentExp;
    }
}