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
    private static readonly int IsSummoningParam = Animator.StringToHash("IsSummoning");

    [Tooltip("Tên state Howl trong Animator Controller (Base Layer) — dùng để kiểm tra animator đã thật sự rời state Howl chưa.")]
    [SerializeField] private string _howlStateName = "Howl";

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
        // Reset trigger Howl nếu còn "treo" — tránh trường hợp Howl bị đè ngược lại
        // bởi 1 trigger AttackClaw cũ chưa tiêu thụ, và đảm bảo 2 action không lẫn nhau.
        _animator.ResetTrigger(HowlTrigger);
        _animator.SetTrigger(AttackClawTrigger);
    }

    public void TriggerHowl()
    {
        // Reset trigger AttackClaw nếu còn "treo" từ lần trước chưa được Animator tiêu thụ
        // (ví dụ do Any State -> AttackClaw chưa kịp transition) — đây là nguyên nhân khiến
        // animation Howl bị AttackClaw "đè" lên giữa lúc đang triệu hồi.
        _animator.ResetTrigger(AttackClawTrigger);
        _animator.SetTrigger(HowlTrigger);
    }

    /// <summary>
    /// Bật/tắt bool "IsSummoning" trên Animator. Dùng làm điều kiện chặn trên transition
    /// "Any State -> AttackClaw" (và "Any State -> Howl") trong Animator Controller, để
    /// AttackClaw không thể cắt ngang Howl bất kể timing phía code C# có trễ hay không.
    /// PHẢI set true TRƯỚC khi gọi TriggerHowl(), và chỉ set false SAU KHI đã xác nhận
    /// animator thật sự rời khỏi state Howl (xem IsInHowlState).
    /// </summary>
    public void SetSummoning(bool isSummoning) => _animator.SetBool(IsSummoningParam, isSummoning);

    /// <summary>
    /// True nếu Animator (layer 0) hiện đang ở state Howl HOẶC đang transition ra khỏi nó.
    /// Dùng để xác nhận animation Howl đã thật sự kết thúc trên Animator trước khi cho
    /// phép hành động khác (ví dụ AttackClaw) chạy tiếp — không chỉ dựa vào Animation Event
    /// hay khoảng chờ cố định, vì giữa lúc Event bắn và lúc Animator thật sự đổi state vẫn
    /// có thể còn vài frame trễ.
    /// </summary>
    public bool IsInHowlState
    {
        get
        {
            if (_animator == null) return false;
            var info = _animator.GetCurrentAnimatorStateInfo(0);
            if (info.IsName(_howlStateName)) return true;
            return _animator.IsInTransition(0) && _animator.GetNextAnimatorStateInfo(0).IsName(_howlStateName);
        }
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
        _animator.SetBool(IsSummoningParam, false);
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