using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using SimpleSurvival.Input;
using SimpleSurvival.Stats;

namespace SimpleSurvival.AI
{
    public sealed class TurkeyController : BasePassiveCreatureController
    {
        [Header("Refs")]
        [SerializeField] private TurkeyAnimator _turkeyAnimator;
        [SerializeField] private EnemyCorpseHandler _corpseHandler;

        private TurkeyStatsConfig TurkeyConfig => _stats != null ? _stats.EnemyConfig as TurkeyStatsConfig : null;

        private float _eatBlockedUntil;
        private Coroutine _eatRoutine;

        protected override void OnPassiveInitialized()
        {
            if (TurkeyConfig == null)
            {
                Debug.LogError($"[{name}] TurkeyStatsConfig missing", this);
                return;
            }

            if (_turkeyAnimator != null) _turkeyAnimator.ResetForSpawn();

            _agent.speed = TurkeyConfig.MoveSpeed;
            _agent.autoBraking = true;
            _agent.stoppingDistance = 0.2f;

            _eatBlockedUntil = 0f;

            StartCoroutine(VisionDetectionRoutine());
            StartBehaviorLoop();
        }

        private void StartBehaviorLoop()
        {
            if (_eatRoutine != null) StopCoroutine(_eatRoutine);
            _eatRoutine = StartCoroutine(WanderEatLoop());
        }

        private IEnumerator WanderEatLoop()
        {
            while (!_isDead)
            {
                if (TurkeyConfig == null) yield break;
                if (_state == PassiveState.Fleeing || _state == PassiveState.Dead)
                {
                    yield return null;
                    continue;
                }

                bool canEat = Time.time >= _eatBlockedUntil;
                bool willEat = canEat && Random.value < TurkeyConfig.EatChance;

                if (willEat)
                    yield return StartCoroutine(EatRoutine());
                else
                    yield return StartCoroutine(WanderRoutine());

                yield return new WaitForSeconds(Random.Range(0.5f, 1.5f));
            }
        }

        private IEnumerator WanderRoutine()
        {
            if (TurkeyConfig == null) yield break;

            _state = PassiveState.Wandering;
            if (_turkeyAnimator != null) _turkeyAnimator.SetEating(false);

            Vector3 origin = _spawnPoint != null ? _spawnPoint.Position : transform.position;
            Vector3 target = GetRandomNavMeshPoint(origin, TurkeyConfig.WanderRadius);

            _agent.speed = TurkeyConfig.MoveSpeed;
            _agent.SetDestination(target);

            float timeout = 8f, elapsed = 0f;
            float stuckTimer = 0f;

            while (_agent.pathPending || _agent.remainingDistance > 0.3f)
            {
                if (_state == PassiveState.Fleeing || _isDead) yield break;

                elapsed += Time.deltaTime;
                if (elapsed > timeout) break;

                if (!_agent.pathPending && _agent.velocity.magnitude < 0.15f)
                {
                    stuckTimer += Time.deltaTime;
                    if (stuckTimer >= 0.4f)
                    {
                        stuckTimer = 0f;
                        target = GetRandomNavMeshPoint(origin, TurkeyConfig.WanderRadius);
                        _agent.SetDestination(target);
                    }
                }
                else
                {
                    stuckTimer = 0f;
                }

                yield return null;
            }

            _agent.ResetPath();
        }

        private IEnumerator EatRoutine()
        {
            if (TurkeyConfig == null) yield break;

            _agent.ResetPath();
            _state = PassiveState.Grazing;
            if (_turkeyAnimator != null) _turkeyAnimator.SetEating(true);

            float duration = Random.Range(TurkeyConfig.EatMinDuration, TurkeyConfig.EatMaxDuration);
            float elapsed = 0f;

            while (elapsed < duration && _state != PassiveState.Fleeing && !_isDead)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (_turkeyAnimator != null) _turkeyAnimator.SetEating(false);
        }

        private IEnumerator VisionDetectionRoutine()
        {
            while (!_isDead)
            {
                yield return new WaitForSeconds(0.3f);
                if (_state == PassiveState.Fleeing || _state == PassiveState.Dead) continue;
                if (TurkeyConfig == null) continue;

                if (_playerInput != null && _playerInput.IsSneakHeld) continue;

                Collider[] hits = Physics.OverlapSphere(transform.position, TurkeyConfig.DetectionRadius);
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
            if (TurkeyConfig == null) yield break;
            if (_state == PassiveState.Dead || _state == PassiveState.Fleeing) yield break;

            _state = PassiveState.Fleeing;
            if (_turkeyAnimator != null) _turkeyAnimator.SetEating(false);

            _agent.speed = TurkeyConfig.FleeSpeed;

            Vector3 fleeDestination = FindClearFleeDestination(dangerPosition, transform.position, TurkeyConfig.FleeDistance);
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
                        Vector3 reroute = FindClearFleeDestination(dangerPosition, transform.position, TurkeyConfig.FleeDistance);
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
            _agent.speed = TurkeyConfig.MoveSpeed;
            _state = PassiveState.Wandering;

            _eatBlockedUntil = Time.time + TurkeyConfig.EatCooldownAfterFlee;
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

            float rotationSpeed = TurkeyConfig != null ? TurkeyConfig.RotationSpeed : 360f;
            MoveAlongAgentPath(_agent.speed, rotationSpeed);

            if (_turkeyAnimator != null)
                _turkeyAnimator.SetSpeed(_agent.velocity.magnitude);
        }

        protected override void OnDying()
        {
            if (_turkeyAnimator != null)
            {
                _turkeyAnimator.SetEating(false);
                _turkeyAnimator.SetIdle();
                _turkeyAnimator.TriggerDeath();
            }

            foreach (var col in GetComponents<Collider>())
            {
                if (col is CharacterController) continue;
                col.enabled = false;
            }

            if (_corpseHandler != null)
                _corpseHandler.SpawnCorpseLoot(TurkeyConfig?.CorpseLootTable);

            float despawnDelay = TurkeyConfig != null ? TurkeyConfig.DespawnDelay : 120f;
            Destroy(gameObject, despawnDelay);
            if (_spawnPoint != null)
                _spawnPoint.NotifyDespawned(despawnDelay);
        }
    }
}