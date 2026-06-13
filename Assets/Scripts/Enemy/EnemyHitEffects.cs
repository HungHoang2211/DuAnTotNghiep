using UnityEngine;
using SimpleSurvival.Stats;

[RequireComponent(typeof(EnemyStats))]
public class EnemyHitEffects : MonoBehaviour
{
    [SerializeField] private ParticleSystem bloodEffectPrefab;
    [SerializeField] private Transform[] bloodSpawnPoints;

    private EnemyStats _stats;

    private void Awake()
    {
        _stats = GetComponent<EnemyStats>();
    }

    private void OnEnable()
    {
        if (_stats != null) _stats.OnDamagedBy += HandleDamagedBy;
    }

    private void OnDisable()
    {
        if (_stats != null) _stats.OnDamagedBy -= HandleDamagedBy;
    }

    private void HandleDamagedBy(GameObject source)
    {
        SpawnBloodEffect();
    }

    private void SpawnBloodEffect()
    {
        if (bloodEffectPrefab == null || bloodSpawnPoints == null || bloodSpawnPoints.Length == 0) return;

        Transform point = bloodSpawnPoints[Random.Range(0, bloodSpawnPoints.Length)];
        Instantiate(bloodEffectPrefab, point.position, point.rotation);
    }
}