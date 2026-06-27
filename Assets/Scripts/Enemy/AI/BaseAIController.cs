using SimpleSurvival.Core;
using SimpleSurvival.Stats;
using UnityEngine;
using UnityEngine.AI;

namespace SimpleSurvival.AI
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(EnemyStats))]
    public abstract class BaseAIController : MonoBehaviour, ISpawnableEnemy
    {
        [Header("Detection Layers")]
        [SerializeField] protected LayerMask playerLayer;
        [SerializeField] protected LayerMask obstacleLayer;

        protected NavMeshAgent _agent;
        protected CharacterController _characterController;
        protected EnemyStats _stats;
        protected IEnemySpawnPoint _spawnPoint;
        protected Transform _player;
        protected bool _isDead;

        protected virtual void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _characterController = GetComponent<CharacterController>();
            _stats = GetComponent<EnemyStats>();

            if (_stats == null)
            {
                Debug.LogError($"[{name}] Missing EnemyStats component", this);
                return;
            }

            // Hybrid setup: NavMeshAgent chỉ pathfinding, không drive transform
            _agent.updatePosition = false;
            _agent.updateRotation = false;
            _agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;

            _stats.OnDeath += HandleDeath;
            _stats.OnDamagedBy += HandleDamagedBy;
        }

        protected virtual void OnDestroy()
        {
            if (_stats != null)
            {
                _stats.OnDeath -= HandleDeath;
                _stats.OnDamagedBy -= HandleDamagedBy;
            }
        }

        public void Initialize(IEnemySpawnPoint spawnPoint)
        {
            _spawnPoint = spawnPoint;
            ResetState();
            OnInitialized();
        }

        protected virtual void ResetState()
        {
            _isDead = false;
            _player = null;

            _agent.isStopped = false;
            _agent.nextPosition = transform.position;

            var col = GetComponent<Collider>();
            if (col != null) col.enabled = true;
        }

        protected abstract void OnInitialized();
        protected abstract void HandleDeath();
        protected abstract void HandleDamagedBy(GameObject source);

        protected Vector3 GetRandomNavMeshPoint(Vector3 origin, float radius)
        {
            for (int i = 0; i < 10; i++)
            {
                Vector2 c = Random.insideUnitCircle * radius;
                Vector3 candidate = origin + new Vector3(c.x, 0, c.y);
                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, radius, NavMesh.AllAreas))
                    return hit.position;
            }
            return origin;
        }

        protected void FaceTarget(Transform target, float rotationSpeed = 360f)
        {
            if (target == null) return;
            Vector3 dir = target.position - transform.position;
            dir.y = 0;
            if (dir == Vector3.zero) return;
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.LookRotation(dir),
                rotationSpeed * Time.deltaTime);
        }
    }
}