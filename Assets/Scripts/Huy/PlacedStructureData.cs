using System;

namespace SimpleSurvival.SaveLoad
{
    [Serializable]
    public sealed class PlacedStructureData
    {
        public string buildingId;
        public int x;
        public int z;
        public int rotationIndex;
    }
}