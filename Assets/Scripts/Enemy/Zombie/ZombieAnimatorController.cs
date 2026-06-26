using System;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class ZombieAnimatorController : MonoBehaviour
{
    public enum Variant { Normal, Fat }

    [Header("Variant")]
    [Tooltip("Normal = Zombie thường (Bool-based). Fat = ZombieFat (Blend Tree + claw/special/jump).")]
    [SerializeField] private Variant _variant = Variant.Normal;

    private Animator _animator;

    public event Action OnJumpAttackImpact;

    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");

    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private static readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");
    private static readonly int IsHowlingHash = Animator.StringToHash("IsHowling");

    private static readonly int IsAttackingClawHash = Animator.StringToHash("IsAttackingClaw");
    private static readonly int IsSpecialAttackingHash = Animator.StringToHash("IsSpecialAttacking");
    private static readonly int JumpAttackHash = Animator.StringToHash("JumpAttack");

    [Header("Ragdoll")]
    [Tooltip("Kéo tất cả Rigidbody trên bone ragdoll vào đây.")]
    [SerializeField] private Rigidbody[] _ragdollBodies;

    [Tooltip("Kéo tất cả Collider trên bone ragdoll vào đây.")]
    [SerializeField] private Collider[] _ragdollColliders;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        SetRagdollActive(false);
    }

    public void SetWalking(bool active)
    {
        if (_variant == Variant.Fat)
        {
            _animator.SetFloat(IsRunningHash, active ? 1f : 0f);
            return;
        }

        _animator.SetBool(IsWalkingHash, active);
        if (active) _animator.SetBool(IsRunningHash, false);
    }

    public void SetRunning(bool active)
    {
        if (_variant == Variant.Fat)
        {
            _animator.SetFloat(IsRunningHash, active ? 2f : 0f);
            return;
        }

        _animator.SetBool(IsRunningHash, active);
        if (active) _animator.SetBool(IsWalkingHash, false);
    }

    public void SetIdle()
    {
        if (_variant == Variant.Fat)
        {
            _animator.SetFloat(IsRunningHash, 0f);
            return;
        }

        _animator.SetBool(IsWalkingHash, false);
        _animator.SetBool(IsRunningHash, false);
    }

    public void SetHowling(bool active)
    {
        if (_variant == Variant.Fat) return;
        _animator.SetBool(IsHowlingHash, active);
    }

    public void TriggerAttack()
    {
        if (_variant == Variant.Fat) return;
        _animator.SetTrigger(IsAttackingHash);
    }

    public void TriggerAttackClaw()
    {
        if (_variant != Variant.Fat) return;
        _animator.SetTrigger(IsAttackingClawHash);
    }

    public void TriggerSpecialAttack()
    {
        if (_variant != Variant.Fat) return;
        _animator.SetTrigger(IsSpecialAttackingHash);
    }

    public void TriggerJumpAttack()
    {
        if (_variant != Variant.Fat) return;
        _animator.SetTrigger(JumpAttackHash);
    }

    public void JumpAttackImpact() => OnJumpAttackImpact?.Invoke();

    public void CancelAttack()
    {
        if (_variant == Variant.Fat)
        {
            _animator.ResetTrigger(IsAttackingClawHash);
            _animator.ResetTrigger(IsSpecialAttackingHash);
            _animator.SetFloat(IsRunningHash, 2f);
            return;
        }

        _animator.ResetTrigger(IsAttackingHash);
        _animator.CrossFade("movement_free_idle", 0.15f, 0);
    }

    public void TriggerDeath()
    {
        _animator.enabled = false;
        SetRagdollActive(true);
    }

    public void ResetForSpawn()
    {
        SetRagdollActive(false);
        _animator.enabled = true;
        _animator.SetBool(IsDeadHash, false);

        if (_variant == Variant.Fat)
        {
            _animator.SetFloat(IsRunningHash, 0f);
            _animator.ResetTrigger(JumpAttackHash);
        }
        else
        {
            _animator.SetBool(IsHowlingHash, false);
            _animator.SetBool(IsWalkingHash, false);
            _animator.SetBool(IsRunningHash, false);
        }

        _animator.Rebind();
        _animator.Update(0f);
    }

    private void SetRagdollActive(bool active)
    {
        foreach (var rb in _ragdollBodies)
        {
            if (rb == null) continue;
            rb.isKinematic = !active;
        }
        foreach (var col in _ragdollColliders)
        {
            if (col == null) continue;
            col.enabled = active;
        }
    }
}
