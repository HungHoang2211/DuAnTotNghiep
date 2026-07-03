namespace SimpleSurvival.Core
{
    public interface IEnemySpawnPoint
    {
        UnityEngine.Vector3 Position { get; }
        void NotifyDespawned(float despawnDelay);
    }

    public interface ISpawnableEnemy
    {
        void Initialize(IEnemySpawnPoint spawnPoint);
    }
}