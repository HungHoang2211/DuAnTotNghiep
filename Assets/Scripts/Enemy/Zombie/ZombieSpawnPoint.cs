using UnityEngine;
using Xyla.Core;

public class ZombieSpawnPoint : MonoBehaviour
{
    [SerializeField] private GameObject _zombiePrefab;
    [SerializeField] private float _respawnDelay = 10f;

    private GameObject _currentZombie;

    private void Start() => SpawnZombie();

    public void SpawnZombie()
    {
        _currentZombie = ObjectPool.Instance.Get(
            _zombiePrefab, transform.position, transform.rotation);

        var controller = _currentZombie.GetComponent<ZombieController>();
        if (controller != null)
            controller.Initialize(this);
    }

    public void OnZombieDespawned()
    {
        Invoke(nameof(SpawnZombie), _respawnDelay);
    }
}