using UnityEngine;

[RequireComponent(typeof(Animator))]
public class DeerAnimatorController : MonoBehaviour
{
    private Animator _animator;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");
    private static readonly int IsGrazingHash = Animator.StringToHash("IsGrazing");

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    public void SetSpeed(float speed)
    {
        _animator.SetFloat(SpeedHash, speed);
    }

    public void SetDead(bool isDead)
    {
        _animator.SetBool(IsDeadHash, isDead);
    }
    public void SetGrazing(bool grazing)
    {
        _animator.SetBool(IsGrazingHash, grazing);
    }    
}