using UnityEngine;
using SimpleSurvival.Combat;
using SimpleSurvival.Core;

/// <summary>
/// Đạn axit của ZombieFat. Nhắm vào vị trí player lúc bắn rồi bay THẲNG
/// theo hướng đó (không đuổi theo player), chạm thì gây damage.
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
    private Vector3 _fixedDirection;
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
        // Tính hướng bay 1 LẦN DUY NHẤT lúc bắn, dựa trên vị trí target hiện tại.
        // Sau đó acid bay thẳng theo hướng này, không bám/đuổi theo target nữa.
        if (target != null)
        {
            Vector3 dir = (target.position + Vector3.up * 0.8f - transform.position).normalized;
            _fixedDirection = dir.sqrMagnitude > 0.0001f ? dir : transform.forward;
        }
        else
        {
            _fixedDirection = transform.forward;
        }

        transform.rotation = Quaternion.LookRotation(_fixedDirection);

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
        _hasHit = false;
    }

    private void Update()
    {
        if (!_initialized) return;

        if (Time.time - _spawnTime >= _lifetime)
        {
            ReturnToPool();
            return;
        }

        // Bay thẳng theo hướng đã chốt lúc bắn — không đuổi theo player.
        transform.position += _fixedDirection * _speed * Time.deltaTime;
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