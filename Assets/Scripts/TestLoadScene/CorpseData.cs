using System;
using System.Collections.Generic;

namespace SimpleSurvival.SaveLoad
{
    [Serializable]
    public sealed class CorpseData
    {
        public string mapId;
        public float x;
        public float y;
        public float z;
        public List<ItemStackData> items = new List<ItemStackData>();
    }
}