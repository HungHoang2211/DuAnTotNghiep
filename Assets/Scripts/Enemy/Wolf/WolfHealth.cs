using UnityEngine;
using Xyla.Combat;
using Xyla.Core;

public class WolfHealth : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    [SerializeField] private float _maxHealth = 150f;

    [Header("Audio (optional)")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _hitClip;
    [SerializeField] private AudioClip _deathClip;

    public float CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    private WolfController _wolfController;
    private WolfLoot _wolfLoot;

    private void Awake()
    {
        CurrentHealth = _maxHealth;
        _wolfController = GetComponent<WolfController>();
        _wolfLoot = GetComponent<WolfLoot>();
    }

    private void OnSpawnFromPool()
    {
        CurrentHealth = _maxHealth;
        IsDead = false;

        var col = GetComponent<Collider>();
        if (col != null) col.enabled = true;

        if (_wolfLoot != null) _wolfLoot.SetLootable(false);
    }

    public bool TakeDamage(float amount, GameObject source)
    {
        if (IsDead) return false;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        PlaySound(_hitClip);

        if (CurrentHealth <= 0f)
            WolfDie();
        else
            // Bị đánh nhưng chưa chết → tấn công lại player
            if (_wolfController != null)
            _wolfController.OnTakeDamage(source.transform);

        return !IsDead;
    }

    private void WolfDie()
    {
        if (IsDead) return;
        IsDead = true;

        PlaySound(_deathClip);

        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (_wolfController != null)
            _wolfController.Die();
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
        Gizmos.DrawWireCube(transform.position + Vector3.up * 2f,
            new Vector3(ratio * 2f, 0.1f, 0.1f));
    }
}