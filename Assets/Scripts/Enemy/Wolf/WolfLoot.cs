using UnityEngine;
using Xyla.Core;

/// <summary>
/// Gắn lên Wolf prefab.
/// Khi wolf chết: hiện prompt "E" khi player đứng gần.
/// Player nhấn E → loot ngẫu nhiên (xương, thịt, da...).
/// Không sửa bất kỳ script Player nào.
/// </summary>
public class WolfLoot : MonoBehaviour
{
    [System.Serializable]
    public struct LootEntry
    {
        public string itemName;
        public GameObject itemPrefab;
        [Range(0f, 1f)]
        public float dropChance;
        public int minAmount;
        public int maxAmount;
    }

    [Header("Loot Table")]
    [SerializeField] private LootEntry[] _lootTable;

    [Header("Interaction")]
    [Tooltip("Khoảng cách player phải đứng để thấy prompt E.")]
    [SerializeField] private float _interactRadius = 2f;
    [Tooltip("Key nhấn để loot. Mặc định E.")]
    [SerializeField] private KeyCode _interactKey = KeyCode.E;

    [Header("UI Prompt")]
    [Tooltip("Kéo GameObject chứa Text 'E' vào đây. " +
             "Tạo Canvas → Text '[ E ] Lấy đồ' → kéo vào.")]
    [SerializeField] private GameObject _promptUI;

    [Header("Loot Collider (optional)")]
    [Tooltip("Collider riêng dùng để detect loot (trigger). Nếu không có thì dùng OverlapSphere.")]
    [SerializeField] private Collider _lootCollider;

    [Header("Drop Settings")]
    [Tooltip("Tầm văng item khi loot.")]
    [SerializeField] private float _dropScatter = 0.6f;
    [Tooltip("Dùng ObjectPool thay vì Instantiate (nếu item prefab có PooledObject).")]
    [SerializeField] private bool _usePool = false;

    private bool _isLootable = false;
    private bool _looted = false;
    private Transform _playerInRange = null;

    // ── API gọi từ WolfController ──────────────────────────

    public void SetLootable(bool lootable)
    {
        _isLootable = lootable;
        _looted = false;

        if (_lootCollider != null)
            _lootCollider.enabled = lootable;

        // Ẩn prompt khi chưa lootable
        if (!lootable) HidePrompt();
    }

    // ── Lifecycle ──────────────────────────────────────────

    private void Update()
    {
        if (!_isLootable || _looted) return;

        CheckPlayerProximity();

        // Nếu player đang trong tầm và nhấn E
        if (_playerInRange != null && Input.GetKeyDown(_interactKey))
            PerformLoot();
    }

    private void CheckPlayerProximity()
    {
        // Tìm player gần nhất trong bán kính
        Collider[] hits = Physics.OverlapSphere(transform.position, _interactRadius);
        _playerInRange = null;

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                _playerInRange = hit.transform;
                break;
            }
        }

        // Hiện/ẩn prompt theo khoảng cách
        if (_playerInRange != null)
            ShowPrompt();
        else
            HidePrompt();
    }

    // ── Loot ──────────────────────────────────────────────

    private void PerformLoot()
    {
        if (_looted || !_isLootable) return;
        _looted = true;

        HidePrompt();

        Vector3 dropPos = transform.position + Vector3.up * 0.1f;

        foreach (var entry in _lootTable)
        {
            if (entry.itemPrefab == null) continue;
            if (Random.value > entry.dropChance) continue;

            int amount = Random.Range(entry.minAmount, entry.maxAmount + 1);
            for (int i = 0; i < amount; i++)
            {
                Vector3 scatter = dropPos
                    + new Vector3(
                        Random.Range(-_dropScatter, _dropScatter),
                        0f,
                        Random.Range(-_dropScatter, _dropScatter));

                if (_usePool)
                    ObjectPool.Instance.Get(entry.itemPrefab, scatter, Quaternion.identity);
                else
                    Instantiate(entry.itemPrefab, scatter, Quaternion.identity);

                Debug.Log($"[WolfLoot] Dropped: {entry.itemName}");
            }
        }

        SetLootable(false);
        Debug.Log("[WolfLoot] Đã loot xong xác sói.");
    }

    // ── UI ────────────────────────────────────────────────

    private void ShowPrompt()
    {
        if (_promptUI != null) _promptUI.SetActive(true);
    }

    private void HidePrompt()
    {
        if (_promptUI != null) _promptUI.SetActive(false);
    }

    // ── Gọi từ PlayerInteraction cũ (giữ tương thích) ─────

    public void OnPlayerLoot(Vector3 dropPosition)
    {
        PerformLoot();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, _interactRadius);
    }
}