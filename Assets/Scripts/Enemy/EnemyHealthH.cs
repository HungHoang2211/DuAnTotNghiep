using UnityEngine;
using SimpleSurvival.Combat;

public class EnemyHealthH : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    [SerializeField] private float _maxHealth = 100f;

    public float CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    private ZombieController _zombie;
    private ZombieFatController _zombieFat;
    private WolfController _wolf;
    private DeerController _deer;

    private int _lastDamageFrame = -1;

    private void Awake()
    {
        CurrentHealth = _maxHealth;
        _zombie = GetComponent<ZombieController>();
        _zombieFat = GetComponent<ZombieFatController>();
        _wolf = GetComponent<WolfController>();
        _deer = GetComponent<DeerController>();
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

        if (CurrentHealth <= 0f)
            Die();
        else if (source != null)
            NotifyTakeDamage(source.transform);

        return !IsDead;
    }

    private void Die()
    {
        if (IsDead) return;
        IsDead = true;

        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (_zombie != null) _zombie.Die();
        else if (_zombieFat != null) _zombieFat.Die();
        else if (_wolf != null) _wolf.Die();
        else if (_deer != null) _deer.Die();
    }

    private void NotifyTakeDamage(Transform attacker)
    {
        if (_zombie != null) _zombie.OnTakeDamage(attacker);
        else if (_zombieFat != null) _zombieFat.OnTakeDamage(attacker);
        else if (_wolf != null) _wolf.OnTakeDamage(attacker);
        // DeerController không có OnTakeDamage — nai chỉ bỏ chạy khi bị tấn công
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