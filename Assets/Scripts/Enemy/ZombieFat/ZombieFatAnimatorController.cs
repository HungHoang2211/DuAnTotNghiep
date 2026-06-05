using UnityEngine;

/// <summary>
/// Điều khiển Animator của ZombieFat.
/// - Movement state: Blend Tree 1D (Idle/Run) theo IsRunning
/// - Attack thường: Attack_Claw_1 → Attack_Claw_2 (tay trái rồi tay phải)
/// - Attack Special: Attack_Special (phun axit từ miệng, sau 10 giây)
/// - Death: Ragdoll + rơi bộ phận ngẫu nhiên
/// </summary>
[RequireComponent(typeof(Animator))]
public class ZombieFatAnimatorController : MonoBehaviour
{
    private Animator _animator;

    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private static readonly int IsAttackingClawHash = Animator.StringToHash("IsAttackingClaw");
    private static readonly int IsSpecialAttackingHash = Animator.StringToHash("IsSpecialAttacking");
    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");

    [Header("Ragdoll")]
    [Tooltip("Kéo tất cả Rigidbody trên bone ragdoll vào đây.")]
    [SerializeField] private Rigidbody[] _ragdollBodies;

    [Tooltip("Kéo tất cả Collider trên bone ragdoll vào đây.")]
    [SerializeField] private Collider[] _ragdollColliders;

    [Header("Detachable Parts")]
    [Tooltip("Các bộ phận rơi khi chết. Có thể là GameObject thường hoặc có SkinnedMeshRenderer.")]
    [SerializeField] private GameObject[] _detachableParts;

    [Tooltip("Lực văng ra khi bộ phận rơi.")]
    [SerializeField] private float _detachForce = 4f;

    [Tooltip("Xác suất rơi bộ phận khi chết (0=không bao giờ, 1=luôn luôn).")]
    [SerializeField][Range(0f, 1f)] private float _detachChance = 0.6f;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        SetRagdollActive(false);
    }

    // ── Animation API ──────────────────────────────────────

    /// <summary>Walk — IsRunning = 1f (Blend Tree: 0=Idle, 1=Walk, 2=Run).</summary>
    public void SetWalking(bool active)
        => _animator.SetFloat(IsRunningHash, active ? 1f : 0f);

    /// <summary>Run — IsRunning = 2f.</summary>
    public void SetRunning(bool active)
        => _animator.SetFloat(IsRunningHash, active ? 2f : 0f);

    /// <summary>Idle — IsRunning = 0f.</summary>
    public void SetIdle()
        => _animator.SetFloat(IsRunningHash, 0f);

    /// <summary>Kích hoạt combo tấn công claw (Attack_Claw_1 → Attack_Claw_2).</summary>
    public void TriggerAttackClaw() => _animator.SetTrigger(IsAttackingClawHash);

    /// <summary>Kích hoạt animation tấn công đặc biệt (phun axit).</summary>
    public void TriggerSpecialAttack() => _animator.SetTrigger(IsSpecialAttackingHash);

    /// <summary>Kích hoạt ragdoll death.</summary>
    public void TriggerDeath()
    {
        _animator.enabled = false;
        SetRagdollActive(true);
        TryDetachRandomPart();
    }

    public void ResetForSpawn()
    {
        SetRagdollActive(false);
        _animator.enabled = true;
        _animator.SetBool(IsDeadHash, false);
        _animator.SetFloat(IsRunningHash, 0f);
        _animator.Rebind();
        _animator.Update(0f);
        ReattachParts();
    }

    // ── Ragdoll ────────────────────────────────────────────

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

    // ── Detach Parts ───────────────────────────────────────

    private void TryDetachRandomPart()
    {
        if (_detachableParts == null || _detachableParts.Length == 0) return;
        if (Random.value > _detachChance) return;

        int index = Random.Range(0, _detachableParts.Length);
        GameObject part = _detachableParts[index];
        if (part == null) return;

        var smr = part.GetComponent<SkinnedMeshRenderer>();
        if (smr != null)
            DetachSkinnedMesh(smr);
        else
            DetachRegularObject(part);
    }

    private void DetachSkinnedMesh(SkinnedMeshRenderer smr)
    {
        Mesh bakedMesh = new Mesh();
        smr.BakeMesh(bakedMesh);

        GameObject detached = new GameObject(smr.gameObject.name + "_Detached");
        detached.transform.SetPositionAndRotation(smr.transform.position, smr.transform.rotation);

        var mf = detached.AddComponent<MeshFilter>();
        var mr = detached.AddComponent<MeshRenderer>();
        mf.mesh = bakedMesh;
        mr.materials = smr.materials;

        var mc = detached.AddComponent<MeshCollider>();
        mc.convex = true;
        mc.sharedMesh = bakedMesh;

        var rb = detached.AddComponent<Rigidbody>();
        rb.mass = 0.8f;
        rb.linearDamping = 0.3f;

        Vector3 dir = (Vector3.up * 1.5f + Random.insideUnitSphere).normalized;
        rb.AddForce(dir * _detachForce, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * _detachForce, ForceMode.Impulse);

        smr.enabled = false;
        Destroy(bakedMesh, 30f);
        Destroy(detached, 30f);
    }

    private void DetachRegularObject(GameObject part)
    {
        part.transform.SetParent(null);

        var rb = part.GetComponent<Rigidbody>();
        if (rb == null) rb = part.AddComponent<Rigidbody>();

        var col = part.GetComponent<Collider>();
        if (col != null) col.enabled = true;

        Vector3 dir = (Vector3.up + Random.insideUnitSphere).normalized;
        rb.AddForce(dir * _detachForce, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * _detachForce, ForceMode.Impulse);

        Destroy(part, 30f);
    }

    private void ReattachParts()
    {
        if (_detachableParts == null) return;
        foreach (var part in _detachableParts)
        {
            if (part == null) continue;
            var smr = part.GetComponent<SkinnedMeshRenderer>();
            if (smr != null) smr.enabled = true;
        }
    }
}