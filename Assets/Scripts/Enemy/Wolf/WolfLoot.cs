using UnityEngine;

public class WolfLoot : MonoBehaviour
{
    [System.Serializable]
    public struct LootEntry
    {
        public string itemName;
        public GameObject itemPrefab;
        [Range(0f, 1f)] public float dropChance;
        public int minAmount;
        public int maxAmount;
    }

    [SerializeField] private LootEntry[] _lootTable;
    [SerializeField] private Collider _lootCollider;

    private bool _isLootable = false;

    public void SetLootable(bool lootable)
    {
        _isLootable = lootable;
        if (_lootCollider != null)
            _lootCollider.enabled = lootable;
    }

    public void OnPlayerLoot(Vector3 dropPosition)
    {
        if (!_isLootable) return;

        foreach (var entry in _lootTable)
        {
            if (Random.value <= entry.dropChance)
            {
                int amount = Random.Range(entry.minAmount, entry.maxAmount + 1);
                for (int i = 0; i < amount; i++)
                {
                    Vector3 scatter = dropPosition + Random.insideUnitSphere * 0.5f;
                    scatter.y = dropPosition.y;
                    Instantiate(entry.itemPrefab, scatter, Quaternion.identity);
                }
            }
        }

        SetLootable(false);
        Debug.Log("[WolfLoot] Đã loot xong xác sói.");
    }
}