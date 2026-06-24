using UnityEngine;

/// <summary>
/// Gộp từ DeerAnimatorController + WolfAnimatorController.
/// Các method dùng chung (SetSpeed, SetDead) được giữ logic tổng quát nhất
/// (theo bản Wolf, vì Deer chỉ là trường hợp riêng với _deathClipCount = 2).
/// Các method riêng của từng loài (SetGrazing - Deer | TriggerAttack/SetHowling/OnAttackHit - Wolf)
/// vẫn giữ nguyên, animal nào không dùng thì đơn giản là không gọi tới.
/// </summary>
[RequireComponent(typeof(Animator))]
public class AnimalAnimatorController : MonoBehaviour
{
    private Animator _animator;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");
    private static readonly int DeathIndexHash = Animator.StringToHash("DeathIndex");
    private static readonly int IsGrazingHash = Animator.StringToHash("IsGrazing"); // Deer
    private static readonly int IsAttackHash = Animator.StringToHash("IsAttacking"); // Wolf
    private static readonly int IsHowlingHash = Animator.StringToHash("IsHowling"); // Wolf

    [Header("Death Variation")]
    [Tooltip("Số lượng animation death trong Blend Tree (mỗi clip cách nhau 1 đơn vị). " +
             "Deer dùng 2 (DeerDie_1/DeerDie_2), Wolf dùng số clip tương ứng trong Animator của Wolf.")]
    [HideInInspector] private int _deathClipCount = 2;

    [Header("Animation Speed Matching (chỉ Deer dùng — Wolf để mặc định 1/1 sẽ không bị ảnh hưởng)")]
    [Tooltip("Chỉnh đến khi bước chân khớp với tốc độ đi bộ")]
    [HideInInspector] private float _walkAnimSpeed = 1f;

    [Tooltip("Chỉnh đến khi bước chân khớp với tốc độ chạy")]
    [HideInInspector] private float _runAnimSpeed = 1f;

    [Tooltip("Ngưỡng tốc độ phân biệt walk và run — phải khớp với transition Speed trong Animator. " +
             "Với Wolf, để mặc định 1/1 thì giá trị này không gây ảnh hưởng.")]
    [HideInInspector] private float _runThreshold = 3.5f;

    private void Awake() => _animator = GetComponent<Animator>();

    /// <summary>Dùng chung cho mọi loài. Với Wolf giữ _walkAnimSpeed = _runAnimSpeed = 1f để không đổi hành vi cũ.</summary>
    public void SetSpeed(float speed)
    {
        _animator.SetFloat(SpeedHash, speed);

        if (speed > _runThreshold)
            _animator.speed = _runAnimSpeed;
        else if (speed > 0.1f)
            _animator.speed = _walkAnimSpeed;
        else
            _animator.speed = 1f;
    }

    /// <summary>
    /// Kích hoạt death với animation ngẫu nhiên qua Blend Tree.
    /// DeathIndex được set TRƯỚC khi IsDead → Blend Tree nhận đúng clip.
    /// Threshold layout: 0, 1, 2, ... (_deathClipCount - 1).
    /// (Deer trước đây random 0/1 thủ công — tương đương Random.Range(0, 2) nên gộp về 1 logic chung.)
    /// </summary>
    public void SetDead(bool isDead)
    {
        if (isDead)
        {
            float randomIndex = Random.Range(0, _deathClipCount); // int range → exact threshold
            _animator.SetFloat(DeathIndexHash, randomIndex);
            _animator.speed = 1f; // reset speed về bình thường
        }

        _animator.SetBool(IsDeadHash, isDead);
    }

    // ===================== Deer-specific =====================
    public void SetGrazing(bool grazing)
    {
        _animator.SetBool(IsGrazingHash, grazing);
        if (grazing) _animator.speed = 1f;
    }

    // ===================== Wolf-specific =====================
    public void TriggerAttack() => _animator.SetTrigger(IsAttackHash);
    public void SetHowling(bool isHowling) => _animator.SetBool(IsHowlingHash, isHowling);

    // Gọi trong Animation Event của clip Attack khi cú đánh chạm
    public void OnAttackHit() { }
}