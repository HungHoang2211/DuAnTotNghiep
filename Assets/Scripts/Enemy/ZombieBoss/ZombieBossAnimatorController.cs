using UnityEngine;

public class ZombieBossAnimatorController : MonoBehaviour
{
    private Animator _animator;

    private Rigidbody[] _ragdollRigidbodies;
    private Collider[] _ragdollColliders;

    private static readonly int MoveSpeedParam = Animator.StringToHash("MoveSpeed");
    private static readonly int AttackClawTrigger = Animator.StringToHash("AttackClaw");
    private static readonly int JumpAttackTrigger = Animator.StringToHash("JumpAttack");
    private static readonly int HowlTrigger = Animator.StringToHash("Howl");
    private static readonly int DeathTrigger = Animator.StringToHash("Death");

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
        _ragdollColliders = GetComponentsInChildren<Collider>();
        SetRagdollEnabled(false);
    }

    public void SetMoveSpeed(float speed) =>
        _animator.SetFloat(MoveSpeedParam, speed, 0.1f, Time.deltaTime);

    public void TriggerAttackClaw() => _animator.SetTrigger(AttackClawTrigger);
    public void TriggerJumpAttack() => _animator.SetTrigger(JumpAttackTrigger);
    public void TriggerHowl() => _animator.SetTrigger(HowlTrigger);
    public void TriggerDeath() => _animator.SetTrigger(DeathTrigger);

    public void ResetForSpawn()
    {
        _animator.SetFloat(MoveSpeedParam, 0f);
        _animator.ResetTrigger(AttackClawTrigger);
        _animator.ResetTrigger(JumpAttackTrigger);
        _animator.ResetTrigger(HowlTrigger);
        _animator.ResetTrigger(DeathTrigger);
        _animator.Play("Movement", 0, 0f);
    }

    public void ActivateRagdoll(Vector3 forceDirection)
    {
        if (_animator != null) _animator.enabled = false;
        SetRagdollEnabled(true);

        Rigidbody hip = GetHipRigidbody();
        if (hip != null)
            hip.AddForce(forceDirection * 5f, ForceMode.Impulse);
    }

    private void SetRagdollEnabled(bool enabled)
    {
        foreach (var rb in _ragdollRigidbodies)
        {
            if (rb.gameObject == gameObject) continue;
            rb.isKinematic = !enabled;
        }

        foreach (var col in _ragdollColliders)
        {
            if (col.gameObject == gameObject) continue;
            col.enabled = enabled;
        }
    }

    private Rigidbody GetHipRigidbody()
    {
        foreach (var rb in _ragdollRigidbodies)
        {
            if (rb.gameObject != gameObject) return rb;
        }
        return null;
    }
}