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

    [Header("Encounter")]
    [Tooltip("Nếu false: enemy từ spawn point này chết sẽ KHÔNG tự spawn lại. " +
             "Dùng cho các spawn point cố định phục vụ 1 sự kiện (vd Witch Event) " +
             "để có thể đếm được khi nào đã giết hết enemy trên map.")]
    [SerializeField] private bool autoRespawn = true;

    [Tooltip("Nếu false: spawn point này KHÔNG tự Spawn() lúc scene Start — " +
             "phải gọi Spawn() thủ công từ nơi khác (vd WitchEventTrap khi trigger, EscortEnemyDirector).")]
    [SerializeField] private bool spawnOnStart = true;

    public Vector3 Position => transform.position;

    /// <summary>
    /// Bắn ra mỗi khi enemy được spawn từ điểm này chết (dù sau đó có tự respawn lại hay không).
    /// Dùng cho các hệ thống cần đếm số enemy đã bị tiêu diệt (vd WitchEventEncounter).
    /// </summary>
    public event System.Action OnEnemyDefeated;

    /// <summary>
    /// Bắn ra ngay sau khi 1 enemy vừa được spawn + Initialize() xong tại điểm này.
    /// Dùng cho các hệ thống cần thao tác trực tiếp lên instance vừa spawn (vd EscortEnemyDirector
    /// gọi SetEscortTarget() để enemy chỉ bám/tấn công NPC hộ tống thay vì Player).
    /// </summary>
    public event System.Action<GameObject> OnEnemySpawned;

    private GameObject _currentEnemy;
    public GameObject CurrentEnemy => _currentEnemy;

    private void Start()
    {
        if (spawnOnStart) Spawn();
    }

    public void Spawn()
    {
        var prefab = PickRandomPrefab();
        if (prefab == null)
        {
            Debug.LogError($"[{name}] Chưa gán prefab nào trong _enemyEntries.", this);
            return;
        }

        if (ObjectPool.Instance == null)
        {
            Debug.LogError($"[{name}] ObjectPool.Instance đang null.", this);
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

        OnEnemySpawned?.Invoke(_currentEnemy);
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
        OnEnemyDefeated?.Invoke();

        if (!autoRespawn) return;

        StartCoroutine(RespawnAfter(despawnDelay));
    }

    private IEnumerator RespawnAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        Spawn();
    }
}