using System;
using UnityEngine;

/// <summary>
/// Điều khiển Animator chung cho Zombie thường và ZombieFat.
/// Chọn biến thể qua <see cref="Variant"/> (set trong Inspector trên từng prefab):
///
/// - Zombie (Variant.Normal): không dùng Blend Tree.
///   Dùng Bool riêng cho từng state: IsWalking, IsRunning, IsHowling, IsAttacking, IsDead.
///   Cancel attack bằng CrossFade về state "movement_free_idle".
///
/// - ZombieFat (Variant.Fat):
///   - Movement state: Blend Tree 1D (Idle/Run) theo IsRunning (0=Idle, 1=Walk, 2=Run)
///   - Attack thường: Attack_Claw_1 → Attack_Claw_2 (tay trái rồi tay phải)
///   - Attack Special: Attack_Special (phun axit từ miệng)
///   - Jump Attack: JumpAttack (giậm chân tại chỗ gây choáng)
///   - Cancel attack bằng cách set IsRunning = 2 để kích hoạt exit transition.
///
/// Cả hai biến thể đều dùng chung: Death (Ragdoll) và ResetForSpawn.
/// </summary>
[RequireComponent(typeof(Animator))]
public class ZombieAnimatorController : MonoBehaviour
{
    public enum Variant { Normal, Fat }

    [Header("Variant")]
    [Tooltip("Normal = Zombie thường (Bool-based). Fat = ZombieFat (Blend Tree + claw/special/jump).")]
    [SerializeField] private Variant _variant = Variant.Normal;

    private Animator _animator;

    /// <summary>
    /// [Chỉ dùng cho Variant.Fat] Được gọi bởi Animation Event tại frame chân chạm đất
    /// trong clip JumpAttack. ZombieFatController đăng ký vào đây để xử lý damage + stun + spawn effect.
    /// </summary>
    public event Action OnJumpAttackImpact;

    // ── Hash chung ─────────────────────────────────────────
    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");

    // ── Hash riêng cho Variant.Normal ───────────────────────
    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private static readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");
    private static readonly int IsHowlingHash = Animator.StringToHash("IsHowling");

    // ── Hash riêng cho Variant.Fat ───────────────────────────
    private static readonly int IsAttackingClawHash = Animator.StringToHash("IsAttackingClaw");
    private static readonly int IsSpecialAttackingHash = Animator.StringToHash("IsSpecialAttacking");
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

    // ── Animation API — Locomotion ──────────────────────────

    /// <summary>
    /// Walk.
    /// - Normal: bật bool IsWalking, tắt IsRunning.
    /// - Fat: IsRunning (float) = 1f (Blend Tree: 0=Idle, 1=Walk, 2=Run).
    /// </summary>
    public void SetWalking(bool active)
    {
        if (_variant == Variant.Fat)
        {
            _animator.SetFloat(IsRunningHash, active ? 1f : 0f);
            return;
        }

        _animator.SetBool(IsWalkingHash, active);
        if (active) _animator.SetBool(IsRunningHash, false);
    }

    /// <summary>
    /// Run.
    /// - Normal: bật bool IsRunning, tắt IsWalking.
    /// - Fat: IsRunning (float) = 2f.
    /// </summary>
    public void SetRunning(bool active)
    {
        if (_variant == Variant.Fat)
        {
            _animator.SetFloat(IsRunningHash, active ? 2f : 0f);
            return;
        }

        _animator.SetBool(IsRunningHash, active);
        if (active) _animator.SetBool(IsWalkingHash, false);
    }

    /// <summary>
    /// Về Idle.
    /// - Normal: tắt cả walk và run (bool).
    /// - Fat: IsRunning (float) = 0f.
    /// </summary>
    public void SetIdle()
    {
        if (_variant == Variant.Fat)
        {
            _animator.SetFloat(IsRunningHash, 0f);
            return;
        }

        _animator.SetBool(IsWalkingHash, false);
        _animator.SetBool(IsRunningHash, false);
    }

    /// <summary>[Chỉ Variant.Normal] Bật/tắt animation howl.</summary>
    public void SetHowling(bool active)
    {
        if (_variant == Variant.Fat) return;
        _animator.SetBool(IsHowlingHash, active);
    }

    // ── Animation API — Attack ──────────────────────────────

    /// <summary>
    /// [Chỉ Variant.Normal] Kích hoạt animation tấn công.
    /// </summary>
    public void TriggerAttack()
    {
        if (_variant == Variant.Fat) return;
        _animator.SetTrigger(IsAttackingHash);
    }

    /// <summary>
    /// [Chỉ Variant.Fat] Kích hoạt combo tấn công claw (Attack_Claw_1 → Attack_Claw_2).
    /// </summary>
    public void TriggerAttackClaw()
    {
        if (_variant != Variant.Fat) return;
        _animator.SetTrigger(IsAttackingClawHash);
    }

    /// <summary>[Chỉ Variant.Fat] Kích hoạt animation tấn công đặc biệt (phun axit).</summary>
    public void TriggerSpecialAttack()
    {
        if (_variant != Variant.Fat) return;
        _animator.SetTrigger(IsSpecialAttackingHash);
    }

    /// <summary>[Chỉ Variant.Fat] Kích hoạt animation JumpAttack (giậm chân tại chỗ).</summary>
    public void TriggerJumpAttack()
    {
        if (_variant != Variant.Fat) return;
        _animator.SetTrigger(JumpAttackHash);
    }

    /// <summary>
    /// [Chỉ Variant.Fat] Gọi method này từ Animation Event tại frame chân chạm đất
    /// trong clip JumpAttack. ZombieFatController sẽ xử lý damage, stun và spawn effect tại đây.
    /// </summary>
    public void JumpAttackImpact() => OnJumpAttackImpact?.Invoke();

    /// <summary>
    /// Huỷ attack giữa chừng khi player thoát tầm đánh.
    /// - Normal: reset trigger + CrossFade về state "movement_free_idle" để khớp NavMesh đang di chuyển.
    /// - Fat: reset trigger + set IsRunning = 2 để kích hoạt exit transition (IsRunning > 0.5)
    ///   đã setup trong Animator Controller.
    /// </summary>
    public void CancelAttack()
    {
        if (_variant == Variant.Fat)
        {
            _animator.ResetTrigger(IsAttackingClawHash);
            _animator.ResetTrigger(IsSpecialAttackingHash);
            _animator.SetFloat(IsRunningHash, 2f); // kích hoạt condition exit về Locomotion
            return;
        }

        _animator.ResetTrigger(IsAttackingHash);
        // Về đúng tên state gốc trong Animator Controller
        _animator.CrossFade("movement_free_idle", 0.15f, 0);
    }

    // ── Animation API — Death / Spawn (dùng chung) ──────────

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

        if (_variant == Variant.Fat)
        {
            _animator.SetFloat(IsRunningHash, 0f);
            _animator.ResetTrigger(JumpAttackHash);
        }
        else
        {
            _animator.SetBool(IsHowlingHash, false);
            _animator.SetBool(IsWalkingHash, false);
            _animator.SetBool(IsRunningHash, false);
        }

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
