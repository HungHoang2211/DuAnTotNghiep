using System;

namespace SimpleSurvival.SaveLoad
{
    [Serializable]
    public sealed class PlayerPlacementData
    {
        public float x;
        public float y;
        public float z;
        public float yaw;
    }

    [Serializable]
    public sealed class PlayerData
    {
        public PlayerStatsData stats = new PlayerStatsData();
        public EquipmentData equipment = new EquipmentData();
        public PlayerInventoryData inventory = new PlayerInventoryData();
        public PlayerPlacementData placement = new PlayerPlacementData();
    }
}