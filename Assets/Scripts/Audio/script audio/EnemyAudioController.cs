using UnityEngine;
using UnityEngine.AI;
using SimpleSurvival.Stats;

namespace SimpleSurvival.Audio
{
    public class EnemyAudioController : MonoBehaviour
    {
        [Header("Combat Cues")]
        [SerializeField] private AudioCue hurtCue;
        [SerializeField] private AudioCue deathCue;

        [Header("Footstep Cue")]
        [SerializeField] private AudioCue footstepCue;
        [SerializeField] private float moveSpeedThreshold = 0.1f;

        [Header("References")]
        [SerializeField] private EnemyStats stats;
        [SerializeField] private NavMeshAgent agent;

        private bool _wasMoving;

        private void Awake()
        {
            if (stats == null) stats = GetComponentInParent<EnemyStats>();
            if (agent == null) agent = GetComponentInParent<NavMeshAgent>();
        }

        private void OnEnable()
        {
            if (stats == null) return;
            stats.OnDamagedBy += HandleDamaged;
            stats.OnDeath += HandleDeath;
        }

        private void OnDisable()
        {
            if (stats == null) return;
            stats.OnDamagedBy -= HandleDamaged;
            stats.OnDeath -= HandleDeath;

            StopFootstepIfNeeded();
        }

        private void Update()
        {
            TickFootstep();
        }

        private void TickFootstep()
        {
            if (agent == null || footstepCue == null || AudioManager.Instance == null) return;
            if (stats != null && stats.IsDead)
            {
                StopFootstepIfNeeded();
                return;
            }
            if (!agent.enabled)
            {
                StopFootstepIfNeeded();
                return;
            }

            bool isMoving = agent.velocity.magnitude >= moveSpeedThreshold;

            if (isMoving && !_wasMoving)
                AudioManager.Instance.StartLoop(footstepCue);
            else if (!isMoving && _wasMoving)
                AudioManager.Instance.StopLoop(footstepCue);

            _wasMoving = isMoving;
        }

        private void StopFootstepIfNeeded()
        {
            if (!_wasMoving) return;
            if (AudioManager.Instance != null && footstepCue != null)
                AudioManager.Instance.StopLoop(footstepCue);
            _wasMoving = false;
        }

        private void HandleDamaged(GameObject attacker)
        {
            PlaySfx(hurtCue);
        }

        private void HandleDeath(GameObject source)
        {
            PlaySfx(deathCue);
            StopFootstepIfNeeded();
        }

        private void PlaySfx(AudioCue cue)
        {
            if (cue == null || AudioManager.Instance == null) return;
            AudioManager.Instance.PlaySfxAt(cue, transform.position);
        }
    }
}