using UnityEngine;

/// <summary>
/// Điều khiển Animator của Zombie — không dùng Blend Tree.
/// Dùng Bool riêng cho từng state: IsWalking, IsRunning, IsHowling, IsAttacking, IsDead.
/// </summary>
[RequireComponent(typeof(Animator))]
public class ZombieAnimatorController : MonoBehaviour
{
    private Animator _animator;

    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private static readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");
    private static readonly int IsHowlingHash = Animator.StringToHash("IsHowling");
    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");

    [Header("Ragdoll")]
    [Tooltip("Kéo vào tất cả Rigidbody trên các bone của ragdoll.")]
    [SerializeField] private Rigidbody[] _ragdollBodies;

    [Tooltip("Kéo vào tất cả Collider trên các bone của ragdoll.")]
    [SerializeField] private Collider[] _ragdollColliders;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        SetRagdollActive(false);
    }

    // ── Animation API ──────────────────────────────────────

    /// <summary>Bật walk, tắt run.</summary>
    public void SetWalking(bool active)
    {
        _animator.SetBool(IsWalkingHash, active);
        if (active) _animator.SetBool(IsRunningHash, false);
    }

    /// <summary>Bật run, tắt walk.</summary>
    public void SetRunning(bool active)
    {
        _animator.SetBool(IsRunningHash, active);
        if (active) _animator.SetBool(IsWalkingHash, false);
    }

    /// <summary>Về Idle — tắt cả walk và run.</summary>
    public void SetIdle()
    {
        _animator.SetBool(IsWalkingHash, false);
        _animator.SetBool(IsRunningHash, false);
    }

    public void SetHowling(bool active) => _animator.SetBool(IsHowlingHash, active);
    public void TriggerAttack() => _animator.SetTrigger(IsAttackingHash);

    /// <summary>
    /// Huỷ giữa chừng animation attack: reset trigger + cross-fade về locomotion.
    /// Gọi khi player thoát tầm đánh để animator khớp với NavMesh đang di chuyển.
    /// </summary>
    public void CancelAttack()
    {
        _animator.ResetTrigger(IsAttackingHash);
        // Về đúng tên state gốc trong Animator Controller
        _animator.CrossFade("movement_free_idle", 0.15f, 0);
    }

    public void TriggerDeath()
    {
        _animator.enabled = false;
        SetRagdollActive(true);
    }

    public void ResetForSpawn()
    {
        SetRagdollActive(false);
        _animator.enabled = true;
        _animator.SetBool(IsDeadHash, false);
        _animator.SetBool(IsHowlingHash, false);
        _animator.SetBool(IsWalkingHash, false);
        _animator.SetBool(IsRunningHash, false);
        _animator.Rebind();
        _animator.Update(0f);
    }

    // ── Ragdoll ────────────────────────────────────────────

    private void SetRagdollActive(bool active)
    {
        foreach (var rb in _ragdollBodies)
        {
            if (rb == null) continue;
            rb.isKinematic = !active;
        }
        foreach (var col in _ragdollColliders)
        {
            if (col == null) continue;
            col.enabled = active;
        }
    }

}