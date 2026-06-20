using System.Collections;
using SimpleSurvival.Combat;
using SimpleSurvival.Core;
using UnityEngine;

/// <summary>
/// Laser AOE rơi từ trên trời xuống vị trí player tại thời điểm kích hoạt.
/// Player có thể né nếu di chuyển ra khỏi vùng _hitRadius trước khi laser đánh xuống.
/// Dùng PooledObject để tái sử dụng — KHÔNG Destroy, tự trả về pool khi animation xong.
/// 
/// Setup prefab:
///   - Gắn script này lên root prefab Laser AOE
///   - Prefab phải có PooledObject component (lifetime = 0 để script tự kiểm soát)
///   - Particle System phải là con của prefab
/// </summary>
[RequireComponent(typeof(PooledObject))]
public class ZombieBossSkill : MonoBehaviour
{
    [Header("Timing")]
    [Tooltip("Thời gian báo hiệu (laser hiển thị) trước khi gây damage (giây)")]
    [SerializeField] private float _warningDuration = 1f;

    [Tooltip("Thời gian laser tồn tại sau khi đánh xuống — chờ animation kết thúc (giây)")]
    [SerializeField] private float _activeDuration = 1.2f;

    [Header("Damage")]
    [Tooltip("Damage khi laser đánh trúng player")]
    [SerializeField] private float _damage = 35f;

    [Tooltip("Bán kính vùng đánh của laser AOE")]
    [SerializeField] private float _hitRadius = 1.2f;

    [Header("Refs")]
    [Tooltip("Layer của player để detect damage")]
    [SerializeField] private LayerMask _playerLayer;

    [Tooltip("ParticleSystem con hiển thị phase báo hiệu (màu vàng/cam nhạt)")]
    [SerializeField] private ParticleSystem _warningParticle;

    [Tooltip("ParticleSystem con hiển thị phase đánh xuống (tia laser chính)")]
    [SerializeField] private ParticleSystem _strikeParticle;

    // Runtime
    private GameObject _owner;
    private PooledObject _pooledObject;
    private Coroutine _sequenceCoroutine;

    private void Awake()
    {
        _pooledObject = GetComponent<PooledObject>();
    }

    /// <summary>
    /// Spawn laser tại vị trí player. Gọi ngay sau ObjectPool.Get().
    /// </summary>
    public void Launch(Vector3 targetPosition, GameObject owner)
    {
        _owner = owner;

        // Đặt laser xuống mặt đất tại vị trí player
        transform.position = new Vector3(targetPosition.x, targetPosition.y, targetPosition.z);

        if (_sequenceCoroutine != null)
            StopCoroutine(_sequenceCoroutine);
        _sequenceCoroutine = StartCoroutine(LaserSequence());
    }

    private IEnumerator LaserSequence()
    {
        // === Phase 1: Báo hiệu ===
        // Hiển thị indicator để player có cơ hội né
        if (_warningParticle != null)
        {
            _warningParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _warningParticle.Play();
        }
        if (_strikeParticle != null)
            _strikeParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        yield return new WaitForSeconds(_warningDuration);

        // === Phase 2: Laser đánh xuống + gây damage ===
        if (_warningParticle != null)
            _warningParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (_strikeParticle != null)
        {
            _strikeParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _strikeParticle.Play();
        }

        // Kiểm tra player có còn trong vùng không (nếu né kịp thì không damage)
        ApplyDamage();

        // === Phase 3: Chờ animation kết thúc rồi trả về pool ===
        yield return new WaitForSeconds(_activeDuration);

        ReturnToPool();
    }

    private void ApplyDamage()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _hitRadius, _playerLayer);
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;
            var damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable != null && !damageable.IsDead)
                damageable.TakeDamage(_damage, _owner);
            break; // chỉ damage player 1 lần
        }
    }

    private void ReturnToPool()
    {
        // Dừng tất cả particle trước khi trả về pool
        if (_warningParticle != null)
            _warningParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (_strikeParticle != null)
            _strikeParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        _sequenceCoroutine = null;
        _pooledObject.ReturnToPool();
    }

    // Đề phòng bị trả về pool giữa chừng (ví dụ boss chết)
    private void OnDisable()
    {
        if (_sequenceCoroutine != null)
        {
            StopCoroutine(_sequenceCoroutine);
            _sequenceCoroutine = null;
        }

        if (_warningParticle != null)
            _warningParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (_strikeParticle != null)
            _strikeParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, _hitRadius);
    }
}