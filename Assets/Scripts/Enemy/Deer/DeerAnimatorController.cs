using UnityEngine;

[RequireComponent(typeof(Animator))]
public class DeerAnimatorController : MonoBehaviour
{
    private Animator _animator;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");
    private static readonly int IsGrazingHash = Animator.StringToHash("IsGrazing");
    // Float 0..1 — Blend Tree Death sẽ dùng giá trị này để chọn animation
    // 0 → DeerDie_1 | 1 → DeerDie_2
    private static readonly int DeathIndexHash = Animator.StringToHash("DeathIndex");

    [Header("Animation Speed Matching")]
    [Tooltip("Chỉnh đến khi bước chân khớp với tốc độ đi bộ")]
    [SerializeField] private float _walkAnimSpeed = 1f;

    [Tooltip("Chỉnh đến khi bước chân khớp với tốc độ chạy")]
    [SerializeField] private float _runAnimSpeed = 1f;

    [Tooltip("Ngưỡng tốc độ phân biệt walk và run — phải khớp với transition Speed trong Animator")]
    [SerializeField] private float _runThreshold = 3.5f;

    private void Awake() => _animator = GetComponent<Animator>();

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
    /// Trước khi set IsDead, chọn ngẫu nhiên DeathIndex để Blend Tree
    /// biết phát DeerDie_1 (0) hay DeerDie_2 (1).
    /// </summary>
    public void SetDead(bool isDead)
    {
        if (isDead)
        {
            // Random 0 hoặc 1 — đặt TRƯỚC khi trigger transition
            float randomIndex = Random.value < 0.5f ? 0f : 1f;
            _animator.SetFloat(DeathIndexHash, randomIndex);
            _animator.speed = 1f; // reset speed về bình thường
        }

        _animator.SetBool(IsDeadHash, isDead);
    }

    public void SetGrazing(bool grazing)
    {
        _animator.SetBool(IsGrazingHash, grazing);
        if (grazing) _animator.speed = 1f;
    }
}