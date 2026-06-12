using UnityEngine;
using SimpleSurvival.Combat;
using Xyla.Core;

/// <summary>
/// Đạn axit của ZombieFat. Bay về phía player, chạm thì gây damage.
///
/// SETUP Prefab:
///   1. Tạo GameObject → đặt tên "ZombieFatAcidEffect"
///   2. Thêm ParticleSystem (màu xanh/vàng cho axit)
///   3. Thêm SphereCollider → Is Trigger = true, Radius = 0.25
///   4. Thêm PooledObject + ZombieFatAcidProjectile
///   5. Lưu thành Prefab → kéo vào _acidEffectPrefab của ZombieFatController
/// </summary>
[RequireComponent(typeof(Collider))]
public class ZombieFatAcid : MonoBehaviour
{
    private Transform _target;
    private float _damage;
    private float _speed;
    private float _lifetime;
    private GameObject _source;

    private bool _initialized = false;
    private bool _hasHit = false;
    private float _spawnTime;

    private ParticleSystem _particles;

    private void Awake()
    {
        _particles = GetComponentInChildren<ParticleSystem>();
    }

    public void Initialize(Transform target, float damage, float speed,
                           float lifetime, GameObject source)
    {
        _target = target;
        _damage = damage;
        _speed = speed;
        _lifetime = lifetime;
        _source = source;
        _initialized = true;
        _hasHit = false;
        _spawnTime = Time.time;

        if (_particles != null)
        {
            _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _particles.Play();
            Debug.Log($"[ZombieFatAcid] Particles: isPlaying={_particles.isPlaying}, isEmitting={_particles.isEmitting}, " +
                      $"gameObject.active={_particles.gameObject.activeSelf}, " +
                      $"localScale={_particles.transform.localScale}, " +
                      $"emission.enabled={_particles.emission.enabled}");
        }
        else
        {
            Debug.LogError("[ZombieFatAcid] _particles là NULL! Kiểm tra lại ParticleSystem trên prefab.");
        }
    }

    private void OnSpawnFromPool()
    {
        _initialized = false;
        _hasHit = false;
        _target = null;
    }

    private void Update()
    {
        if (!_initialized) return;

        if (Time.time - _spawnTime >= _lifetime)
        {
            ReturnToPool();
            return;
        }

        // Bay về player với homing nhẹ
        if (_target != null)
        {
            Vector3 dir = (_target.position + Vector3.up * 0.8f - transform.position).normalized;
            transform.position += dir * _speed * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(dir);
        }
        else
        {
            transform.position += transform.forward * _speed * Time.deltaTime;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hasHit || !_initialized) return;
        if (!other.CompareTag("Player")) return;

        _hasHit = true;

        var damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null && !damageable.IsDead)
            damageable.TakeDamage(_damage, _source);

        if (_particles != null) _particles.Stop();
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        _initialized = false;
        ObjectPool.Instance.Return(gameObject);
    }
}