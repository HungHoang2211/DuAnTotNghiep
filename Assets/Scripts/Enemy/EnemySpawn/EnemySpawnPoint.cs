using System.Collections;
using UnityEngine;
using SimpleSurvival.Core;

public class EnemySpawnPoint : MonoBehaviour, IEnemySpawnPoint
{
    [System.Serializable]
    private class EnemyEntry
    {
        [Tooltip("Prefab enemy. Phải có component implement ISpawnableEnemy.")]
        public GameObject Prefab;

        [Tooltip("Trọng số random — số càng lớn càng dễ ra. Ví dụ Wolf=5, Boss=1 thì Boss hiếm hơn Wolf 5 lần.")]
        public float Weight = 1f;
    }

    [Header("Enemy")]
    [Tooltip("Danh sách prefab enemy có thể spawn tại điểm này. Mỗi lần spawn sẽ random chọn 1 theo Weight.")]
    [SerializeField] private EnemyEntry[] _enemyEntries;

    private GameObject _currentEnemy;

    private void Start() => Spawn();

    public void Spawn()
    {
        var prefab = PickRandomPrefab();
        if (prefab == null)
        {
            Debug.LogError($"[{name}] Chưa gán prefab nào trong _enemyEntries.", this);
            return;
        }

        _currentEnemy = ObjectPool.Instance.Get(prefab, transform.position, transform.rotation);

        var spawnable = _currentEnemy.GetComponent<ISpawnableEnemy>();
        if (spawnable != null)
        {
            spawnable.Initialize(this);
        }
        else
        {
            Debug.LogWarning(
                $"[{name}] Prefab '{prefab.name}' không có component implement ISpawnableEnemy — " +
                "enemy sẽ không được Initialize.", this);
        }
    }

    private GameObject PickRandomPrefab()
    {
        if (_enemyEntries == null || _enemyEntries.Length == 0) return null;

        float totalWeight = 0f;
        foreach (var entry in _enemyEntries)
        {
            if (entry != null && entry.Prefab != null && entry.Weight > 0f)
                totalWeight += entry.Weight;
        }
        if (totalWeight <= 0f) return null;

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        foreach (var entry in _enemyEntries)
        {
            if (entry == null || entry.Prefab == null || entry.Weight <= 0f) continue;
            cumulative += entry.Weight;
            if (roll <= cumulative) return entry.Prefab;
        }

        return null;
    }

    public void NotifyDespawned(float despawnDelay)
    {
        StartCoroutine(RespawnAfter(despawnDelay));
    }

    private IEnumerator RespawnAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        Spawn();
    }
}