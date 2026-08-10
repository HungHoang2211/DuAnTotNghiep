using UnityEngine;
using UnityEngine.AI;

namespace SimpleSurvival.AI
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class NPCEmilyMovement : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 3.5f;
        [SerializeField] private float rotationSpeed = 360f;
        [SerializeField] private float arrivalThreshold = 0.5f;
        [SerializeField] private float navMeshSampleRadius = 3f;

        private NavMeshAgent _agent;
        private Rigidbody _rigidbody;
        private NPCEmilyAnimator _animatorController;

        private Vector3 _destination;
        private bool _isMoving;
        private bool _isPaused;

        public bool HasArrived { get; private set; }

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _rigidbody = GetComponent<Rigidbody>();
            _animatorController = GetComponent<NPCEmilyAnimator>();

            _rigidbody.isKinematic = true;
            _rigidbody.useGravity = false;

            _agent.updatePosition = false;
            _agent.updateRotation = false;
            _agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;

            EnsureOnNavMesh();
        }

        public void BeginMoveTo(Vector3 destination)
        {
            if (!EnsureOnNavMesh())
            {
                Debug.LogWarning($"[NPCEmilyMovement] '{name}' không nằm trên NavMesh, bỏ qua lệnh di chuyển.", this);
                return;
            }

            _destination = destination;
            HasArrived = false;
            _isMoving = true;
            _isPaused = false;

            _agent.isStopped = false;
            _agent.SetDestination(_destination);
        }

        public void WarpTo(Vector3 position)
        {
            Stop();

            if (!_agent.Warp(position))
            {
                if (NavMesh.SamplePosition(position, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
                    _agent.Warp(hit.position);
                else
                    transform.position = position;
            }

            _rigidbody.position = transform.position;
            _agent.nextPosition = transform.position;
        }

        public void Stop()
        {
            _isMoving = false;
            if (_agent.isOnNavMesh)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
            }
            _animatorController?.SetMoving(false);
        }

        public void SetPaused(bool paused)
        {
            _isPaused = paused;
            if (paused) _animatorController?.SetMoving(false);
        }

        private bool EnsureOnNavMesh()
        {
            if (_agent.isOnNavMesh)
            {
                _agent.nextPosition = transform.position;
                return true;
            }

            if (_agent.Warp(transform.position)) return true;

            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
                return _agent.Warp(hit.position);

            return false;
        }

        private void FixedUpdate()
        {
            if (!_isMoving || _isPaused || !_agent.isOnNavMesh) return;

            float dist = Vector3.Distance(_rigidbody.position, _destination);
            if (dist <= arrivalThreshold)
            {
                HasArrived = true;
                Stop();
                return;
            }

            Vector3 desiredVel = _agent.desiredVelocity;
            Vector3 move = desiredVel.normalized * moveSpeed;
            _rigidbody.MovePosition(_rigidbody.position + move * Time.fixedDeltaTime);
            _agent.nextPosition = _rigidbody.position;

            Vector3 lookDir = new Vector3(desiredVel.x, 0f, desiredVel.z);
            if (lookDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                Quaternion newRot = Quaternion.RotateTowards(_rigidbody.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
                _rigidbody.MoveRotation(newRot);
            }

            _animatorController?.SetMoving(true);
        }
    }
}