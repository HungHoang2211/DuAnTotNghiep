namespace SimpleSurvival.Items
{
    public interface IHarvestable
    {
        bool CanHarvest { get; }
        void Harvest(PlayerInventory playerInventory, int skillLevel = 0);
    }
}