using UnityEngine;
using Xyla.Combat;

public class ZombieFatHealth : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    [SerializeField] private float _maxHealth = 250f; // fat zombie chịu đòn nhiều hơn

    [Header("Audio (optional)")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _hitClip;
    [SerializeField] private AudioClip _deathClip;

    public float CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    private ZombieFatController _controller;
    private int _lastDamageFrame = -1;

    private void Awake()
    {
        CurrentHealth = _maxHealth;
        _controller = GetComponent<ZombieFatController>();
    }

    private void OnSpawnFromPool()
    {
        CurrentHealth = _maxHealth;
        IsDead = false;
        _lastDamageFrame = -1;

        var col = GetComponent<Collider>();
        if (col != null) col.enabled = true;
    }

    public bool TakeDamage(float amount, GameObject source)
    {
        if (IsDead) return false;
        if (Time.frameCount == _lastDamageFrame) return false;
        _lastDamageFrame = Time.frameCount;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        PlaySound(_hitClip);

        if (CurrentHealth <= 0f)
            ZombieDie();
        else if (_controller != null && source != null)
            _controller.OnTakeDamage(source.transform);

        return !IsDead;
    }

    private void ZombieDie()
    {
        if (IsDead) return;
        IsDead = true;
        PlaySound(_deathClip);

        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (_controller != null) _controller.Die();
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
        Gizmos.DrawWireCube(transform.position + Vector3.up * 2.5f,
            new Vector3(ratio * 2.5f, 0.1f, 0.1f));
    }
}