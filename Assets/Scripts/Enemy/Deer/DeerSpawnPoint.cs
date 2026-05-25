using UnityEngine;
using Xyla.Core;

public class DeerSpawnPoint : MonoBehaviour
{
    [SerializeField] private GameObject _deerPrefab;
    [SerializeField] private float _respawnDelay = 120f; // 2 phút

    private GameObject _currentDeer;

    private void Start()
    {
        SpawnDeer();
    }

    public void SpawnDeer()
    {
        _currentDeer = ObjectPool.Instance.Get(_deerPrefab, transform.position, transform.rotation);

        var controller = _currentDeer.GetComponent<DeerController>();
        if (controller != null)
            controller.Initialize(this);
    }

    public void OnDeerDespawned()
    {
        // Nai đã biến mất, hồi sinh sau _respawnDelay giây
        Invoke(nameof(SpawnDeer), _respawnDelay);
    }
}