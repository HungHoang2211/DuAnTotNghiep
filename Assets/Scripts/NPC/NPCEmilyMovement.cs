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
        }

        public void BeginMoveTo(Vector3 destination)
        {
            _destination = destination;
            HasArrived = false;
            _isMoving = true;
            _isPaused = false;

            _agent.isStopped = false;
            _agent.nextPosition = transform.position;
            _agent.SetDestination(_destination);
        }

        public void WarpTo(Vector3 position)
        {
            Stop();

            transform.position = position;
            _rigidbody.position = position;

            if (_agent.isOnNavMesh) _agent.Warp(position);
            _agent.nextPosition = position;
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