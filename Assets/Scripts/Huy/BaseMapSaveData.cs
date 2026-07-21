using System;
using System.Collections.Generic;

namespace SimpleSurvival.SaveLoad
{
    [Serializable]
    public sealed class BaseMapSaveData
    {
        public List<PlacedStructureData> floors = new List<PlacedStructureData>();
        public List<PlacedStructureData> walls = new List<PlacedStructureData>();
        public List<PlacedStructureData> furniture = new List<PlacedStructureData>();
    }
}