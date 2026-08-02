using UnityEngine;
using UnityEngine.AI;

namespace SimpleSurvival.AI
{
    /// <summary>
    /// Di chuyển Emily tới 1 điểm đích, dùng chung pattern NavMeshAgent (tính đường)
    /// + CharacterController (thực thi di chuyển) như BaseAIController.MoveAlongAgentPath.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(CharacterController))]
    public sealed class NPCEmilyMovement : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 3.5f;
        [SerializeField] private float rotationSpeed = 360f;
        [SerializeField] private float arrivalThreshold = 0.5f;

        private NavMeshAgent _agent;
        private CharacterController _characterController;
        private NPCEmilyAnimator _animatorController;

        private Vector3 _destination;
        private bool _isMoving;
        private bool _isPaused;

        public bool HasArrived { get; private set; }

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _characterController = GetComponent<CharacterController>();
            _animatorController = GetComponent<NPCEmilyAnimator>();

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

            if (_characterController != null) _characterController.enabled = false;

            transform.position = position;

            if (_agent != null)
            {
                if (_agent.isOnNavMesh) _agent.Warp(position);
                _agent.nextPosition = position;
            }

            if (_characterController != null) _characterController.enabled = true;
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

        /// <summary>
        /// Tạm dừng di chuyển (vd lúc bị enemy đánh trúng và đang chơi animation phản đòn).
        /// </summary>
        public void SetPaused(bool paused)
        {
            _isPaused = paused;
            if (paused) _animatorController?.SetMoving(false);
        }

        private void Update()
        {
            if (!_isMoving || _isPaused || !_agent.isOnNavMesh) return;

            float dist = Vector3.Distance(transform.position, _destination);
            if (dist <= arrivalThreshold)
            {
                HasArrived = true;
                Stop();
                return;
            }

            Vector3 desiredVel = _agent.desiredVelocity;
            Vector3 move = desiredVel.normalized * moveSpeed;
            move.y += Physics.gravity.y * Time.deltaTime;
            _characterController.Move(move * Time.deltaTime);
            _agent.nextPosition = transform.position;

            Vector3 lookDir = new Vector3(desiredVel.x, 0f, desiredVel.z);
            if (lookDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }

            _animatorController?.SetMoving(true);
        }
    }
}