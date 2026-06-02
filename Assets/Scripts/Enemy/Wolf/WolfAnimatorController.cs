using UnityEngine;

[RequireComponent(typeof(Animator))]
public class WolfAnimatorController : MonoBehaviour
{
    private Animator _animator;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");
    private static readonly int IsAttackHash = Animator.StringToHash("IsAttacking");
    private static readonly int IsHowlingHash = Animator.StringToHash("IsHowling");
    // Float 0..1 — Blend Tree Death sẽ dùng giá trị này để chọn animation
    // Ví dụ: 0 → WolfDeath_1 | 0.5 → WolfDeath_2 | 1 → WolfDeath_3
    // Hiện tại nếu chỉ có 1 clip thì thêm clip thứ 2 vào Animator trước
    private static readonly int DeathIndexHash = Animator.StringToHash("DeathIndex");

    // Số lượng death animation trong Blend Tree (chỉnh khi thêm/bớt clip)
    [Header("Death Variation")]
    [Tooltip("Số lượng animation death trong Blend Tree (mỗi clip cách nhau 1 đơn vị)")]
    [SerializeField] private int _deathClipCount = 2;

    private void Awake() => _animator = GetComponent<Animator>();

    public void SetSpeed(float speed) => _animator.SetFloat(SpeedHash, speed);

    /// <summary>
    /// Kích hoạt death với animation ngẫu nhiên qua Blend Tree.
    /// DeathIndex được set TRƯỚC khi IsDead → Blend Tree nhận đúng clip.
    /// </summary>
    public void SetDead(bool isDead)
    {
        if (isDead)
        {
            // Random chọn 1 trong _deathClipCount clip
            // Threshold layout: 0, 1, 2, ... (_deathClipCount - 1)
            float randomIndex = Random.Range(0, _deathClipCount); // int range → exact threshold
            _animator.SetFloat(DeathIndexHash, randomIndex);
        }

        _animator.SetBool(IsDeadHash, isDead);
    }

    public void TriggerAttack() => _animator.SetTrigger(IsAttackHash);
    public void SetHowling(bool isHowling) => _animator.SetBool(IsHowlingHash, isHowling);

    // Gọi trong Animation Event của clip Attack khi cú đánh chạm
    public void OnAttackHit() { }
}