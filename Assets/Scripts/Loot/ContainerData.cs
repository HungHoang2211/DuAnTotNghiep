using System;

namespace SimpleSurvival.SaveLoad
{
    [Serializable]
    public sealed class ContainerData
    {
        public string containerId;
        public InventoryData inventory = new InventoryData();
    }
}