using UnityEngine;
using SimpleSurvival.Core;

public class ZombieBossSpawnPoint : MonoBehaviour
{
    [SerializeField] private GameObject _bossPrefab;
    [SerializeField] private float _respawnDelay = 60f;

    private void Start() => SpawnBoss();

    public void SpawnBoss()
    {
        var obj = ObjectPool.Instance.Get(_bossPrefab, transform.position, transform.rotation);
        var controller = obj.GetComponent<ZombieBossController>();
        if (controller != null)
            controller.Initialize(this);
    }

    public void OnBossDespawned()
    {
        Invoke(nameof(SpawnBoss), _respawnDelay);
    }
}