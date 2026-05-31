using UnityEngine;

[RequireComponent(typeof(Animator))]
public class DeerAnimatorController : MonoBehaviour
{
    private Animator _animator;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");
    private static readonly int IsGrazingHash = Animator.StringToHash("IsGrazing");

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
            _animator.speed = 1f; // đứng yên → tốc độ mặc định
    }

    public void SetDead(bool isDead)
    {
        _animator.SetBool(IsDeadHash, isDead);
        if (isDead) _animator.speed = 1f; // reset về bình thường khi chết
    }

    public void SetGrazing(bool grazing)
    {
        _animator.SetBool(IsGrazingHash, grazing);
        if (grazing) _animator.speed = 1f; // animation ăn cỏ không cần scale
    }
}