namespace SimpleSurvival.Targets
{
    public interface IUnlockable : ITargetable
    {
        float UnlockDuration { get; }
        void MarkUnlocked();
        void Open();
    }
}