using UnityEngine;
using Xyla.Core;

public class ZombieFatSpawnPoint : MonoBehaviour
{
    [SerializeField] private GameObject _zombieFatPrefab;
    [SerializeField] private float _respawnDelay = 15f;

    private GameObject _currentZombie;

    private void Start() => SpawnZombieFat();

    public void SpawnZombieFat()
    {
        _currentZombie = ObjectPool.Instance.Get(
            _zombieFatPrefab, transform.position, transform.rotation);

        var controller = _currentZombie.GetComponent<ZombieFatController>();
        if (controller != null)
            controller.Initialize(this);
    }

    public void OnZombieFatDespawned()
    {
        Invoke(nameof(SpawnZombieFat), _respawnDelay);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, 0.6f);
        Gizmos.DrawRay(transform.position, transform.forward * 1.5f);
    }
}