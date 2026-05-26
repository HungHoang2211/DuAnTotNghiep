using UnityEngine;

[RequireComponent(typeof(Animator))]
public class WolfAnimatorController : MonoBehaviour
{
    private Animator _animator;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");
    private static readonly int IsAttackHash = Animator.StringToHash("IsAttacking");
    private static readonly int IsHowlingHash = Animator.StringToHash("IsHowling");

    private void Awake() => _animator = GetComponent<Animator>();

    public void SetSpeed(float speed) => _animator.SetFloat(SpeedHash, speed);
    public void SetDead(bool isDead) => _animator.SetBool(IsDeadHash, isDead);
    public void TriggerAttack() => _animator.SetTrigger(IsAttackHash);
    public void SetHowling(bool isHowling) => _animator.SetBool(IsHowlingHash, isHowling);

    // Gọi trong Animation Event của clip Attack khi cú đánh chạm
    public void OnAttackHit() { }
}