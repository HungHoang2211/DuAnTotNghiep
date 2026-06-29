namespace SimpleSurvival.Core
{
    public interface IEnemySpawnPoint
    {
        void NotifyDespawned(float despawnDelay);
    }

    public interface ISpawnableEnemy
    {
        void Initialize(IEnemySpawnPoint spawnPoint);
    }
}