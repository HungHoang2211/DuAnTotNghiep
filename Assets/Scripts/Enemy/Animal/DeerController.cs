using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using SimpleSurvival.Input;
using SimpleSurvival.Stats;

namespace SimpleSurvival.AI
{
    public sealed class DeerController : BasePassiveCreatureController
    {
        [Header("Refs")]
        [SerializeField] private DeerAnimator _deerAnimator;
        [SerializeField] private EnemyCorpseHandler _corpseHandler;

        private DeerStatsConfig DeerConfig => _stats != null ? _stats.EnemyConfig as DeerStatsConfig : null;

        private float _grazeBlockedUntil;
        private Coroutine _grazeRoutine;

        protected override void OnPassiveInitialized()
        {
            if (DeerConfig == null)
            {
                Debug.LogError($"[{name}] DeerStatsConfig missing", this);
                return;
            }

            if (_deerAnimator != null) _deerAnimator.ResetForSpawn();

            _agent.speed = DeerConfig.MoveSpeed;
            _agent.autoBraking = true;
            _agent.stoppingDistance = 0.2f;

            _grazeBlockedUntil = 0f;

            StartCoroutine(VisionDetectionRoutine());
            StartBehaviorLoop();
        }

        private void StartBehaviorLoop()
        {
            if (_grazeRoutine != null) StopCoroutine(_grazeRoutine);
            _grazeRoutine = StartCoroutine(WanderGrazeLoop());
        }

        private IEnumerator WanderGrazeLoop()
        {
            while (!_isDead)
            {
                if (DeerConfig == null) yield break;
                if (_state == PassiveState.Fleeing || _state == PassiveState.Dead)
                {
                    yield return null;
                    continue;
                }

                bool canGraze = Time.time >= _grazeBlockedUntil;
                bool willGraze = canGraze && Random.value < DeerConfig.GrazeChance;

                if (willGraze)
                    yield return StartCoroutine(GrazeRoutine());
                else
                    yield return StartCoroutine(WanderRoutine());

                yield return new WaitForSeconds(Random.Range(0.5f, 1.5f));
            }
        }

        private IEnumerator WanderRoutine()
        {
            if (DeerConfig == null) yield break;

            _state = PassiveState.Wandering;
            if (_deerAnimator != null) _deerAnimator.SetGrazing(false);

            Vector3 origin = _spawnPoint != null ? _spawnPoint.Position : transform.position;
            Vector3 target = GetRandomNavMeshPoint(origin, DeerConfig.WanderRadius);

            _agent.speed = DeerConfig.MoveSpeed;
            _agent.SetDestination(target);

            float timeout = 8f, elapsed = 0f;
            float stuckCheckTimer = 0f;
            Vector3 lastCheckPos = transform.position;

            while (_agent.pathPending || _agent.remainingDistance > 0.3f)
            {
                if (_state == PassiveState.Fleeing || _isDead) yield break;
                elapsed += Time.deltaTime;
                if (elapsed > timeout) break;

                stuckCheckTimer += Time.deltaTime;
                if (stuckCheckTimer >= 0.4f)
                {
                    float movedDist = Vector3.Distance(transform.position, lastCheckPos);
                    if (!_agent.pathPending && movedDist < 0.15f)
                    {
                        target = GetRandomNavMeshPoint(origin, DeerConfig.WanderRadius);
                        _agent.SetDestination(target);
                    }
                    lastCheckPos = transform.position;
                    stuckCheckTimer = 0f;
                }

                yield return null;
            }

            _agent.ResetPath();
        }

        private IEnumerator GrazeRoutine()
        {
            if (DeerConfig == null) yield break;

            _agent.ResetPath();
            _state = PassiveState.Grazing;
            if (_deerAnimator != null) _deerAnimator.SetGrazing(true);

            float duration = Random.Range(DeerConfig.GrazeMinDuration, DeerConfig.GrazeMaxDuration);
            float elapsed = 0f;

            while (elapsed < duration && _state != PassiveState.Fleeing && !_isDead)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (_deerAnimator != null) _deerAnimator.SetGrazing(false);
        }
        private IEnumerator VisionDetectionRoutine()
        {
            while (!_isDead)
            {
                yield return new WaitForSeconds(0.3f);
                if (_state == PassiveState.Fleeing || _state == PassiveState.Dead) continue;
                if (DeerConfig == null) continue;

                if (_playerInput != null && _playerInput.IsSneakHeld) continue;

                Collider[] hits = Physics.OverlapSphere(transform.position, DeerConfig.DetectionRadius);
                foreach (var hit in hits)
                {
                    if (!hit.CompareTag("Player")) continue;
                    StartCoroutine(FleeFrom(hit.transform.position));
                    break;
                }
            }
        }

        protected override IEnumerator FleeFrom(Vector3 dangerPosition)
        {
            if (DeerConfig == null) yield break;
            if (_state == PassiveState.Dead || _state == PassiveState.Fleeing) yield break;

            _state = PassiveState.Fleeing;
            if (_deerAnimator != null) _deerAnimator.SetGrazing(false);

            _agent.speed = DeerConfig.FleeSpeed;

            Vector3 fleeDestination = FindClearFleeDestination(dangerPosition, transform.position, DeerConfig.FleeDistance);
            _agent.SetDestination(fleeDestination);

            float timeout = 5f, elapsed = 0f;
            float stuckTimer = 0f;

            while (_agent.pathPending || _agent.remainingDistance > 0.5f)
            {
                if (_state != PassiveState.Fleeing || _isDead) yield break;

                elapsed += Time.deltaTime;
                if (elapsed > timeout) break;

                if (!_agent.pathPending && _agent.velocity.magnitude < 0.15f)
                {
                    stuckTimer += Time.deltaTime;
                    if (stuckTimer >= 0.4f)
                    {
                        stuckTimer = 0f;
                        Vector3 reroute = FindClearFleeDestination(dangerPosition, transform.position, DeerConfig.FleeDistance);
                        _agent.SetDestination(reroute);
                    }
                }
                else
                {
                    stuckTimer = 0f;
                }

                yield return null;
            }

            _agent.ResetPath();
            _agent.speed = DeerConfig.MoveSpeed;
            _state = PassiveState.Wandering;

            _grazeBlockedUntil = Time.time + DeerConfig.GrazeCooldownAfterFlee;
        }

        private Vector3 FindClearFleeDestination(Vector3 dangerPosition, Vector3 origin, float fleeDistance)
        {
            Vector3 baseDir = (origin - dangerPosition).normalized;
            if (baseDir == Vector3.zero) baseDir = transform.forward;

            float[] angleOffsets = { 0f, 30f, -30f, 60f, -60f, 90f, -90f, 135f, -135f };
            Vector3 rayOrigin = origin + Vector3.up * 0.5f;

            foreach (float angle in angleOffsets)
            {
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * baseDir;

                if (obstacleLayer != 0 &&
                    Physics.Raycast(rayOrigin, dir, Mathf.Min(fleeDistance, 3f), obstacleLayer))
                {
                    continue;
                }

                Vector3 candidate = origin + dir * fleeDistance;
                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, fleeDistance, NavMesh.AllAreas))
                    return hit.position;
            }

            if (NavMesh.SamplePosition(origin + baseDir * fleeDistance, out NavMeshHit fallbackHit, fleeDistance, NavMesh.AllAreas))
                return fallbackHit.position;

            return origin;
        }

        private void Update()
        {
            if (_isDead) return;

            float rotationSpeed = DeerConfig != null ? DeerConfig.RotationSpeed : 360f;
            MoveAlongAgentPath(_agent.speed, rotationSpeed);

            if (_deerAnimator != null)
                _deerAnimator.SetSpeed(_agent.velocity.magnitude);
        }

        protected override void OnDying()
        {
            if (_deerAnimator != null)
            {
                _deerAnimator.SetGrazing(false);
                _deerAnimator.SetIdle();
                _deerAnimator.TriggerDeath();
            }

            foreach (var c in GetComponentsInChildren<Collider>())
                c.enabled = false;

            if (_corpseHandler != null)
                _corpseHandler.SpawnCorpseLoot(DeerConfig?.CorpseLootTable);

            float despawnDelay = DeerConfig != null ? DeerConfig.DespawnDelay : 120f;
            Destroy(gameObject, despawnDelay);
            if (_spawnPoint != null)
                _spawnPoint.NotifyDespawned(despawnDelay);
        }
    }
}