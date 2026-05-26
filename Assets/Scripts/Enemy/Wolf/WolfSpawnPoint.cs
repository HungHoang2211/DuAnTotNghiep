using UnityEngine;
using Xyla.Core;

public class WolfSpawnPoint : MonoBehaviour
{
    [SerializeField] private GameObject _wolfPrefab;
    [SerializeField] private float _respawnDelay = 60f;

    private GameObject _currentWolf;

    private void Start() => SpawnWolf();

    public void SpawnWolf()
    {
        _currentWolf = ObjectPool.Instance.Get(
            _wolfPrefab, transform.position, transform.rotation);

        var controller = _currentWolf.GetComponent<WolfController>();
        if (controller != null)
            controller.Initialize(this);
    }

    public void OnWolfDespawned()
    {
        Invoke(nameof(SpawnWolf), _respawnDelay);
    }
}