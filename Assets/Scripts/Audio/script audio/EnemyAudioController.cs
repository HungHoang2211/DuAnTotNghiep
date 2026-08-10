using SimpleSurvival.Stats;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace SimpleSurvival.Audio
{
    public class EnemyAudioController : MonoBehaviour
    {
        [Header("Combat Cues")]
        [SerializeField] private AudioCue hurtCue;
        [SerializeField] private AudioCue deathCue;

        [Header("Footstep Cue")]
        [SerializeField] private AudioCue footstepCue;

        // Thời gian chờ SAU KHI cue trước phát xong
        [SerializeField] private float footstepDelay = 1.5f;

        [SerializeField] private float moveSpeedThreshold = 0.1f;

        [Header("References")]
        [SerializeField] private EnemyStats stats;
        [SerializeField] private NavMeshAgent agent;

        private Coroutine _footstepCoroutine;

        private void Awake()
        {
            if (stats == null)
                stats = GetComponentInParent<EnemyStats>();

            if (agent == null)
                agent = GetComponentInParent<NavMeshAgent>();
        }

        private void OnEnable()
        {
            if (stats != null)
            {
                stats.OnDamagedBy += HandleDamaged;
                stats.OnDeath += HandleDeath;
            }
        }

        private void OnDisable()
        {
            if (stats != null)
            {
                stats.OnDamagedBy -= HandleDamaged;
                stats.OnDeath -= HandleDeath;
            }

            StopFootsteps();
        }

        private void Update()
        {
            if (agent == null)
                return;

            if (stats != null && stats.IsDead)
            {
                StopFootsteps();
                return;
            }

            if (!agent.enabled)
            {
                StopFootsteps();
                return;
            }

            bool isMoving = agent.velocity.magnitude >= moveSpeedThreshold;

            if (isMoving)
            {
                StartFootsteps();
            }
            else
            {
                StopFootsteps();
            }
        }

        private void StartFootsteps()
        {
            if (_footstepCoroutine != null)
                return;

            if (footstepCue == null || AudioManager.Instance == null)
                return;

            _footstepCoroutine = StartCoroutine(FootstepRoutine());
        }

        private void StopFootsteps()
        {
            if (_footstepCoroutine != null)
            {
                StopCoroutine(_footstepCoroutine);
                _footstepCoroutine = null;
            }
        }

        private IEnumerator FootstepRoutine()
        {
            while (true)
            {
                // Phát 1 lần
                AudioSource source = AudioManager.Instance.PlaySfxAt(
                    footstepCue,
                    transform.position
                );

                // Nếu không phát được thì dừng
                if (source == null)
                    break;

                // CHỜ CUE PHÁT HẾT
                while (source != null && source.isPlaying)
                {
                    yield return null;
                }

                // CUE ĐÃ PHÁT HẾT
                // Bây giờ mới bắt đầu footstepDelay
                float timer = 0f;

                while (timer < footstepDelay)
                {
                    // Nếu Enemy dừng thì dừng luôn
                    if (agent == null ||
                        !agent.enabled ||
                        agent.velocity.magnitude < moveSpeedThreshold)
                    {
                        _footstepCoroutine = null;
                        yield break;
                    }

                    timer += Time.deltaTime;
                    yield return null;
                }

                // Hết delay -> vòng lặp phát cue tiếp
            }

            _footstepCoroutine = null;
        }

        private void HandleDamaged(GameObject attacker)
        {
            PlaySfx(hurtCue);
        }

        private void HandleDeath(GameObject source)
        {
            StopFootsteps();
            PlaySfx(deathCue);
        }

        private void PlaySfx(AudioCue cue)
        {
            if (cue == null || AudioManager.Instance == null)
                return;

            AudioManager.Instance.PlaySfxAt(
                cue,
                transform.position
            );
        }
    }
}
