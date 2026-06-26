using UnityEngine;

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

    public void SetDead(bool isDead)
    {
        if (isDead)
        {
            float randomIndex = Random.Range(0, _deathClipCount);
            _animator.SetFloat(DeathIndexHash, randomIndex);
            _animator.speed = 1f;
        }

        _animator.SetBool(IsDeadHash, isDead);
    }

    public void SetGrazing(bool grazing)
    {
        _animator.SetBool(IsGrazingHash, grazing);
        if (grazing) _animator.speed = 1f;
    }

    public void TriggerAttack() => _animator.SetTrigger(IsAttackHash);
    public void SetHowling(bool isHowling) => _animator.SetBool(IsHowlingHash, isHowling);

    public void OnAttackHit() { }
}