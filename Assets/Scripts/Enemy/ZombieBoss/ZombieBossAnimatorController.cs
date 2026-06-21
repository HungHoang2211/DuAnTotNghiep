using System;
using UnityEngine;

public class ZombieBossAnimatorController : MonoBehaviour
{
    private Animator _animator;

    /// <summary>
    /// Được gọi bởi Animation Event ở frame cuối của clip Howl.
    /// ZombieBossController đăng ký vào đây để biết khi nào howl xong.
    /// </summary>
    public event Action OnHowlFinished;

    /// <summary>
    /// Được gọi bởi Animation Event tại frame muốn spawn minion trong clip Howl.
    /// </summary>
    public event Action OnHowlSpawn;

    private static readonly int MoveSpeedParam = Animator.StringToHash("MoveSpeed");
    private static readonly int AttackClawTrigger = Animator.StringToHash("AttackClaw");
    private static readonly int HowlTrigger = Animator.StringToHash("Howl");
    private static readonly int DeathTrigger = Animator.StringToHash("Death");

    [Header("Ragdoll")]
    [Tooltip("Kéo tất cả Rigidbody trên bone ragdoll vào đây.")]
    [SerializeField] private Rigidbody[] _ragdollBodies;

    [Tooltip("Kéo tất cả Collider trên bone ragdoll vào đây.")]
    [SerializeField] private Collider[] _ragdollColliders;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        SetRagdollEnabled(false);
    }

    public void SetMoveSpeed(float speed) =>
        _animator.SetFloat(MoveSpeedParam, speed, 0.1f, Time.deltaTime);

    public void TriggerAttackClaw()
    {
        // DEBUG TẠM: in stack trace để tìm xem có script nào khác (ngoài
        // ZombieBossController.NormalAttackRoutine) đang gọi hàm này không.
        // Xoá dòng Debug.Log này sau khi đã tìm ra nguyên nhân.
        Debug.Log($"[ZombieBossAnimatorController] TriggerAttackClaw() called at {Time.time:F2}s:\n{System.Environment.StackTrace}");

        // Reset trigger Howl nếu còn "treo" — tránh trường hợp Howl bị đè ngược lại
        // bởi 1 trigger AttackClaw cũ chưa tiêu thụ, và đảm bảo 2 action không lẫn nhau.
        _animator.ResetTrigger(HowlTrigger);
        _animator.SetTrigger(AttackClawTrigger);
    }

    public void TriggerHowl()
    {
        Debug.Log($"[ZombieBossAnimatorController] TriggerHowl() called at {Time.time:F2}s");

        // Reset trigger AttackClaw nếu còn "treo" từ lần trước chưa được Animator tiêu thụ
        // (ví dụ do Any State -> AttackClaw chưa kịp transition) — đây là nguyên nhân khiến
        // animation Howl bị AttackClaw "đè" lên giữa lúc đang triệu hồi.
        _animator.ResetTrigger(AttackClawTrigger);
        _animator.SetTrigger(HowlTrigger);
    }

    public void TriggerDeath() => _animator.SetTrigger(DeathTrigger);

    /// <summary>
    /// Gọi method này từ Animation Event tại frame muốn spawn minion trong clip Howl.
    /// </summary>
    public void HowlSpawn() => OnHowlSpawn?.Invoke();

    /// <summary>
    /// Gọi method này từ Animation Event ở frame cuối của clip Howl trong Animator.
    /// </summary>
    public void HowlFinished() => OnHowlFinished?.Invoke();

    public void ResetForSpawn()
    {
        _animator.enabled = true;
        _animator.SetFloat(MoveSpeedParam, 0f);
        _animator.ResetTrigger(AttackClawTrigger);
        _animator.ResetTrigger(HowlTrigger);
        _animator.ResetTrigger(DeathTrigger);
        _animator.Play("Movement", 0, 0f);
        SetRagdollEnabled(false);
    }

    public void ActivateRagdoll(Vector3 forceDirection)
    {
        if (_animator != null) _animator.enabled = false;
        SetRagdollEnabled(true);

        if (_ragdollBodies != null && _ragdollBodies.Length > 0)
        {
            Rigidbody hip = _ragdollBodies[0];
            if (hip != null)
                hip.AddForce(forceDirection * 5f, ForceMode.Impulse);
        }
    }

    private void SetRagdollEnabled(bool enabled)
    {
        if (_ragdollBodies != null)
        {
            foreach (var rb in _ragdollBodies)
            {
                if (rb == null) continue;
                rb.isKinematic = !enabled;
            }
        }

        if (_ragdollColliders != null)
        {
            foreach (var col in _ragdollColliders)
            {
                if (col == null) continue;
                col.enabled = enabled;
            }
        }
    }
}