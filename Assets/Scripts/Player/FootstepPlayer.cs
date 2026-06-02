using UnityEngine;
using Xyla.Audio;

namespace Xyla.Player
{
    // SETUP:
    //   Gắn lên Player, kéo AudioSource vào _audioSource.
    //   Gắn FootstepSurface lên từng mặt đất (Grass, Wood, Stone...).
    //   Kéo TopDownMover vào _mover để đọc IsMoving / IsRunning / IsSneaking.
    //   Gán _defaultClips và _defaultSneakClips phòng khi mặt đất không có FootstepSurface.
    //
    //  SNEAK FOOTSTEP:
    //   - FootstepSurface hỗ trợ thêm mảng sneakClips; nếu chưa có, dùng _defaultSneakClips.
    //   - Volume scale nhân thêm _sneakVolumeMultiplier (mặc định 0.3) → khẽ hơn rất nhiều.
    //   - Mỗi bước chân gọi PlayerState.EmitNoise() với noise level tương ứng.

    public class FootstepPlayer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TopDownMover _mover;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private PlayerState _playerState;    

        [Header("Timing — Stand")]
        [SerializeField] private float _walkStepInterval = 0.45f;
        [SerializeField] private float _runStepInterval = 0.28f;

        [Header("Timing — Sneak")]
        [SerializeField] private float _sneakStepInterval = 0.55f;  // chậm hơn walk

        [Header("Volume — Sneak")]
        [Tooltip("Nhân vào volume của bước chân khi sneak. " +
                 "Enemy dùng noise level để phát hiện, nên để thấp ~0.25–0.4.")]
        [SerializeField][Range(0f, 1f)] private float _sneakVolumeMultiplier = 0.30f;

        [Header("Raycast")]
        [Tooltip("Độ dài raycast xuống đất.")]
        [SerializeField] private float _rayLength = 1.2f;
        [Tooltip("Layer của mặt đất.")]
        [SerializeField] private LayerMask _groundLayer;

        [Header("Fallback — Stand")]
        [Tooltip("Clip dùng khi mặt đất không có FootstepSurface (đứng).")]
        [SerializeField] private AudioClip[] _defaultClips;
        [SerializeField][Range(0f, 1f)] private float _defaultVolume = 0.7f;

        [Header("Fallback — Sneak")]
        [Tooltip("Clip dùng khi mặt đất không có FootstepSurface (ngồi). " +
                 "Nên dùng tiếng vải/rón rén.")]
        [SerializeField] private AudioClip[] _defaultSneakClips;
        [Tooltip("Volume fallback khi sneak (trước khi nhân _sneakVolumeMultiplier).")]
        [SerializeField][Range(0f, 1f)] private float _defaultSneakVolume = 0.5f;



        private float _nextFootstepTime;

        private void Awake()
        {
            // Tự tìm PlayerState nếu chưa kéo vào
            if (_playerState == null)
                _playerState = GetComponent<PlayerState>();
        }

        private void Update()
        {
            if (_mover == null || _audioSource == null) return;
            if (!_mover.IsMoving)
            {
                _nextFootstepTime = Time.time;
                return;
            }

            if (Time.time < _nextFootstepTime) return;

            PlayFootstep();

            float interval = _mover.IsSneaking ? _sneakStepInterval
                           : _mover.IsRunning ? _runStepInterval
                           : _walkStepInterval;
            _nextFootstepTime = Time.time + interval;
        }

        // ── Playback ──────────────────────────────────────────────────────
        private void PlayFootstep()
        {
            bool sneaking = _mover.IsSneaking;

            bool hitGround = Physics.Raycast(
                transform.position, Vector3.down, out RaycastHit hit,
                _rayLength, _groundLayer, QueryTriggerInteraction.Ignore);

            if (hitGround)
            {
                var surface = hit.collider.GetComponent<FootstepSurface>();
                if (surface != null)
                {
                    AudioClip clip = sneaking
                        ? (surface.GetRandomSneakClip() ?? surface.GetRandomClip())
                        : surface.GetRandomClip();

                    if (clip != null)
                    {
                        float vol = sneaking
                            ? surface.VolumeScale * _sneakVolumeMultiplier
                            : surface.VolumeScale;

                        _audioSource.PlayOneShot(clip, vol);
                        EmitNoise(sneaking);
                        return;
                    }
                }
            }

            if (sneaking)
                PlayDefaultSneakFootstep();
            else
                PlayDefaultFootstep();
        }

        private void PlayDefaultFootstep()
        {
            if (_defaultClips == null || _defaultClips.Length == 0) return;
            AudioClip clip = _defaultClips[Random.Range(0, _defaultClips.Length)];
            if (clip == null) return;
            _audioSource.PlayOneShot(clip, _defaultVolume);
            EmitNoise(sneaking: false);
        }

        private void PlayDefaultSneakFootstep()
        {
            // Nếu không có clip ngồi riêng → dùng clip đứng nhưng nhân volume
            AudioClip[] pool = (_defaultSneakClips != null && _defaultSneakClips.Length > 0)
                ? _defaultSneakClips
                : _defaultClips;

            if (pool == null || pool.Length == 0) return;
            AudioClip clip = pool[Random.Range(0, pool.Length)];
            if (clip == null) return;
            _audioSource.PlayOneShot(clip, _defaultSneakVolume * _sneakVolumeMultiplier);
            EmitNoise(sneaking: true);
        }

        private void EmitNoise(bool sneaking)
        {
            if (_playerState == null) return;

            float level;
            if (sneaking)
                level = PlayerState.NoiseSneakWalk;
            else if (_mover.IsRunning)
                level = PlayerState.NoiseRun;
            else
                level = PlayerState.NoiseWalk;

            _playerState.EmitNoise(level);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, Vector3.down * _rayLength);
        }
    }
}