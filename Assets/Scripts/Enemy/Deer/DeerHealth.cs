using UnityEngine;
using Xyla.Combat;
using Xyla.Core;

/// <summary>
/// Tự implement IDamageable — KHÔNG kế thừa EnemyHealth.
/// Gắn lên prefab nai THAY THẾ hoàn toàn cho EnemyHealth.
/// </summary>
public class DeerHealth : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    [SerializeField] private float _maxHealth = 100f;

    [Header("On Hit (optional)")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _hitClip;
    [SerializeField] private AudioClip _deathClip;

    // IDamageable
    public float CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    private DeerController _deerController;
    private DeerLoot _deerLoot;

    private void Awake()
    {
        CurrentHealth = _maxHealth;
        _deerController = GetComponent<DeerController>();
        _deerLoot = GetComponent<DeerLoot>();
    }

    // ObjectPool gọi khi spawn lại
    private void OnSpawnFromPool()
    {
        CurrentHealth = _maxHealth;
        IsDead = false;

        var col = GetComponent<Collider>();
        if (col != null) col.enabled = true;

        if (_deerLoot != null) _deerLoot.SetLootable(false);
    }

    public bool TakeDamage(float amount, GameObject source)
    {
        if (IsDead) return false;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);

        if (CurrentHealth <= 0f)
        {
            DeerDie();
        }
        else
        {
            // Chưa chết — phát sound hit + nai bỏ chạy
            PlaySound(_hitClip);
            if (_deerController != null)
                _deerController.OnPlayerInteract(source.transform.position);
        }

        return !IsDead;
    }

    private void DeerDie()
    {
        if (IsDead) return;
        IsDead = true;

        PlaySound(_deathClip);

        // Tắt collider chính — chỉ loot collider còn hoạt động
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Ủy quyền hoàn toàn cho DeerController:
        // → animation dead
        // → bật loot
        // → ReturnDelayed 120 giây (không Destroy)
        // → báo SpawnPoint hồi sinh
        if (_deerController != null)
            _deerController.Die();
    }

    private void PlaySound(AudioClip clip)
    {
        if (_audioSource == null || clip == null) return;
        _audioSource.PlayOneShot(clip);
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        float ratio = CurrentHealth / _maxHealth;
        Gizmos.color = Color.Lerp(Color.red, Color.green, ratio);
        Gizmos.DrawWireCube(
            transform.position + Vector3.up * 2f,
            new Vector3(ratio * 2f, 0.1f, 0.1f));
    }
}