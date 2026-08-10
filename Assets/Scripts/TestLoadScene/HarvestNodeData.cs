using System;

namespace SimpleSurvival.SaveLoad
{
    [Serializable]
    public sealed class HarvestNodeData
    {
        public string nodeId;
        public float hp;
        public bool isDepleted;
    }
}