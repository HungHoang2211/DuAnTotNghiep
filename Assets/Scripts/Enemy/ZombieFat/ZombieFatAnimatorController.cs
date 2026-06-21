using System;
using UnityEngine;

/// <summary>
/// Điều khiển Animator của ZombieFat.
/// - Movement state: Blend Tree 1D (Idle/Run) theo IsRunning
/// - Attack thường: Attack_Claw_1 → Attack_Claw_2 (tay trái rồi tay phải)
/// - Attack Special: Attack_Special (phun axit từ miệng, sau 10 giây)
/// - Jump Attack: JumpAttack (giậm chân tại chỗ gây choáng)
/// - Death: Ragdoll
/// </summary>
[RequireComponent(typeof(Animator))]
public class ZombieFatAnimatorController : MonoBehaviour
{
    private Animator _animator;

    /// <summary>
    /// Được gọi bởi Animation Event tại frame chân chạm đất trong clip JumpAttack.
    /// ZombieFatController đăng ký vào đây để xử lý damage + stun + spawn effect.
    /// </summary>
    public event Action OnJumpAttackImpact;

    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private static readonly int IsAttackingClawHash = Animator.StringToHash("IsAttackingClaw");
    private static readonly int IsSpecialAttackingHash = Animator.StringToHash("IsSpecialAttacking");
    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");
    private static readonly int JumpAttackHash = Animator.StringToHash("JumpAttack");

    [Header("Ragdoll")]
    [Tooltip("Kéo tất cả Rigidbody trên bone ragdoll vào đây.")]
    [SerializeField] private Rigidbody[] _ragdollBodies;

    [Tooltip("Kéo tất cả Collider trên bone ragdoll vào đây.")]
    [SerializeField] private Collider[] _ragdollColliders;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        SetRagdollActive(false);
    }

    // ── Animation API ──────────────────────────────────────

    /// <summary>Walk — IsRunning = 1f (Blend Tree: 0=Idle, 1=Walk, 2=Run).</summary>
    public void SetWalking(bool active)
        => _animator.SetFloat(IsRunningHash, active ? 1f : 0f);

    /// <summary>Run — IsRunning = 2f.</summary>
    public void SetRunning(bool active)
        => _animator.SetFloat(IsRunningHash, active ? 2f : 0f);

    /// <summary>Idle — IsRunning = 0f.</summary>
    public void SetIdle()
        => _animator.SetFloat(IsRunningHash, 0f);

    /// <summary>Kích hoạt combo tấn công claw (Attack_Claw_1 → Attack_Claw_2).</summary>
    public void TriggerAttackClaw() => _animator.SetTrigger(IsAttackingClawHash);

    /// <summary>Kích hoạt animation tấn công đặc biệt (phun axit).</summary>
    public void TriggerSpecialAttack() => _animator.SetTrigger(IsSpecialAttackingHash);

    /// <summary>Kích hoạt animation JumpAttack (giậm chân tại chỗ).</summary>
    public void TriggerJumpAttack() => _animator.SetTrigger(JumpAttackHash);

    /// <summary>
    /// Gọi method này từ Animation Event tại frame chân chạm đất trong clip JumpAttack.
    /// ZombieFatController sẽ xử lý damage, stun và spawn effect tại đây.
    /// </summary>
    public void JumpAttackImpact() => OnJumpAttackImpact?.Invoke();

    /// <summary>
    /// Huỷ attack giữa chừng: reset trigger + set IsRunning = 2 để
    /// kích hoạt exit transition (IsRunning > 0.5) đã setup trong Animator Controller.
    /// </summary>
    public void CancelAttack()
    {
        _animator.ResetTrigger(IsAttackingClawHash);
        _animator.ResetTrigger(IsSpecialAttackingHash);
        _animator.SetFloat(IsRunningHash, 2f); // kích hoạt condition exit về Locomotion
    }

    /// <summary>Kích hoạt ragdoll death.</summary>
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
        _animator.SetFloat(IsRunningHash, 0f);
        _animator.ResetTrigger(JumpAttackHash);
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