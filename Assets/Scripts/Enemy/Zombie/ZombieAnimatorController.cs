using UnityEngine;

/// <summary>
/// Điều khiển Animator của Zombie — không dùng Blend Tree.
/// Dùng Bool riêng cho từng state: IsWalking, IsRunning, IsHowling, IsAttacking, IsDead.
/// </summary>
[RequireComponent(typeof(Animator))]
public class ZombieAnimatorController : MonoBehaviour
{
    private Animator _animator;

    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private static readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");
    private static readonly int IsHowlingHash = Animator.StringToHash("IsHowling");
    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");

    [Header("Ragdoll")]
    [Tooltip("Kéo vào tất cả Rigidbody trên các bone của ragdoll.")]
    [SerializeField] private Rigidbody[] _ragdollBodies;

    [Tooltip("Kéo vào tất cả Collider trên các bone của ragdoll.")]
    [SerializeField] private Collider[] _ragdollColliders;

    [Header("Detachable Parts (optional)")]
    [Tooltip("Các bộ phận có thể rơi ra khi chết.")]
    [SerializeField] private GameObject[] _detachableParts;

    [Tooltip("Lực văng ra khi bộ phận rơi.")]
    [SerializeField] private float _detachForce = 3f;

    [Tooltip("Xác suất rơi bộ phận khi chết (0=không bao giờ, 1=luôn luôn).")]
    [SerializeField][Range(0f, 1f)] private float _detachChance = 0.5f;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        SetRagdollActive(false);
    }

    // ── Animation API ──────────────────────────────────────

    /// <summary>Bật walk, tắt run.</summary>
    public void SetWalking(bool active)
    {
        _animator.SetBool(IsWalkingHash, active);
        if (active) _animator.SetBool(IsRunningHash, false);
    }

    /// <summary>Bật run, tắt walk.</summary>
    public void SetRunning(bool active)
    {
        _animator.SetBool(IsRunningHash, active);
        if (active) _animator.SetBool(IsWalkingHash, false);
    }

    /// <summary>Về Idle — tắt cả walk và run.</summary>
    public void SetIdle()
    {
        _animator.SetBool(IsWalkingHash, false);
        _animator.SetBool(IsRunningHash, false);
    }

    public void SetHowling(bool active) => _animator.SetBool(IsHowlingHash, active);
    public void TriggerAttack() => _animator.SetTrigger(IsAttackingHash);

    /// <summary>
    /// Huỷ giữa chừng animation attack: reset trigger + cross-fade về locomotion.
    /// Gọi khi player thoát tầm đánh để animator khớp với NavMesh đang di chuyển.
    /// </summary>
    public void CancelAttack()
    {
        _animator.ResetTrigger(IsAttackingHash);
        // Về đúng tên state gốc trong Animator Controller
        _animator.CrossFade("movement_free_idle", 0.15f, 0);
    }

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
        _animator.SetBool(IsHowlingHash, false);
        _animator.SetBool(IsWalkingHash, false);
        _animator.SetBool(IsRunningHash, false);
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
        rb.mass = 0.5f;
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