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
        [HideInInspector] protected LayerMask obstacleLayer;

        protected NavMeshAgent _agent;
        protected CharacterController _characterController;
        protected EnemyStats _stats;
        protected IEnemySpawnPoint _spawnPoint;
        protected Transform _player;
        protected bool _isDead;

        protected PlayerStats _playerStats;
        protected bool _playerDead;

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

            // Player là unique trong scene nên tìm 1 lần và lắng nghe OnDeath của player
            if (_playerStats == null)
                _playerStats = FindAnyObjectByType<PlayerStats>();

            if (_playerStats != null)
            {
                _playerDead = _playerStats.IsDead;
                _playerStats.OnDeath += HandlePlayerDeath;
            }
        }

        protected virtual void OnDestroy()
        {
            if (_stats != null)
            {
                _stats.OnDeath -= HandleDeath;
                _stats.OnDamagedBy -= HandleDamagedBy;
            }

            if (_playerStats != null)
                _playerStats.OnDeath -= HandlePlayerDeath;
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

            // Awake() có thể chạy trước khi Player được spawn (enemy pool khởi tạo trước),
            // nên thử tìm + đăng ký lại mỗi lần enemy được Initialize() từ pool.
            if (_playerStats == null)
            {
                _playerStats = FindAnyObjectByType<PlayerStats>();
                if (_playerStats != null)
                    _playerStats.OnDeath += HandlePlayerDeath;
            }
            _playerDead = _playerStats != null && _playerStats.IsDead;

            _agent.isStopped = false;
            _agent.nextPosition = transform.position;

            var col = GetComponent<Collider>();
            if (col != null) col.enabled = true;
        }

        protected abstract void OnInitialized();
        protected abstract void HandleDeath();
        protected abstract void HandleDamagedBy(GameObject source);

        /// <summary>
        /// Gọi khi player chết (ragdoll kích hoạt). Enemy con phải override để
        /// dừng hẳn chase/attack, không cần biết chi tiết implement của player.
        /// </summary>
        protected virtual void HandlePlayerDeath()
        {
            _playerDead = true;
        }

        /// <summary>
        /// Di chuyển GameObject theo path của NavMeshAgent nhưng thực thi
        /// bằng CharacterController (hybrid: NavMeshAgent chỉ pathfinding).
        /// Dùng chung cho cả enemy chủ động (chase) và creature bị động (wander/flee).
        /// </summary>
        protected void MoveAlongAgentPath(float moveSpeed, float rotationSpeed)
        {
            Vector3 desiredVel = _agent.desiredVelocity;
            Vector3 move = desiredVel.normalized * moveSpeed;
            move.y += Physics.gravity.y * Time.deltaTime;
            _characterController.Move(move * Time.deltaTime);
            _agent.nextPosition = transform.position;

            Vector3 lookDir = new Vector3(desiredVel.x, 0, desiredVel.z);
            if (lookDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, targetRot,
                    rotationSpeed * Time.deltaTime);
            }
        }

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