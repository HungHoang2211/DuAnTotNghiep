using SimpleSurvival.Stats;
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

        // AudioSource của tiếng bước chân đang phát
        private AudioSource _footstepSource;

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
            if (Time.timeScale <= 0f)
            {
                StopFootsteps();
                return;
            }

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

            bool isMoving =
                agent.velocity.magnitude >= moveSpeedThreshold;

            if (isMoving)
                StartFootsteps();
            else
                StopFootsteps();
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

            // Dừng ngay clip bước chân đang phát
            if (_footstepSource != null)
            {
                _footstepSource.Stop();
                _footstepSource = null;
            }
        }

        private IEnumerator FootstepRoutine()
        {
            while (true)
            {
                // Phát 1 lần
                _footstepSource = AudioManager.Instance.PlaySfxAt(
                    footstepCue,
                    transform.position
                );

                // Nếu không phát được thì dừng
                if (_footstepSource == null)
                    break;

                // Chờ clip phát xong
                while (_footstepSource != null && _footstepSource.isPlaying)
                {
                    // Nếu đang phát mà Nai dừng thì cắt luôn
                    if (agent == null ||
                        !agent.enabled ||
                        agent.velocity.magnitude < moveSpeedThreshold)
                    {
                        if (_footstepSource != null)
                        {
                            _footstepSource.Stop();
                            _footstepSource = null;
                        }

                        _footstepCoroutine = null;
                        yield break;
                    }

                    yield return null;
                }

                _footstepSource = null;

                // Delay sau khi clip kết thúc
                float timer = 0f;

                while (timer < footstepDelay)
                {
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
            }

            _footstepCoroutine = null;
            _footstepSource = null;
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