using UnityEngine;
using Xyla.Audio;

namespace Xyla.Player
{
    // SETUP:
    //   Gắn lên Player, kéo AudioSource vào _audioSource.
    //   Gắn FootstepSurface lên từng mặt đất (Grass, Wood, Stone...).
    //   Kéo TopDownMover vào _mover để đọc IsMoving / IsRunning.
    //   Gán _defaultClips phòng khi mặt đất không có FootstepSurface.

    public class FootstepPlayer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TopDownMover _mover;
        [SerializeField] private AudioSource _audioSource;

        [Header("Timing")]
        [SerializeField] private float _walkStepInterval = 0.45f;
        [SerializeField] private float _runStepInterval = 0.28f;

        [Header("Raycast")]
        [Tooltip("Độ dài raycast xuống đất. Tăng nếu player bay/nhảy cao.")]
        [SerializeField] private float _rayLength = 1.2f;
        [Tooltip("Layer của mặt đất. Không include Player, Enemy.")]
        [SerializeField] private LayerMask _groundLayer;

        [Header("Fallback")]
        [Tooltip("Clip dùng khi mặt đất không có FootstepSurface.")]
        [SerializeField] private AudioClip[] _defaultClips;
        [SerializeField][Range(0f, 1f)] private float _defaultVolume = 0.7f;

        private float _nextFootstepTime;

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
            float interval = _mover.IsRunning ? _runStepInterval : _walkStepInterval;
            _nextFootstepTime = Time.time + interval;
        }

        private void PlayFootstep()
        {
            // Raycast xuống đất từ vị trí player
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit,
                _rayLength, _groundLayer, QueryTriggerInteraction.Ignore))
            {
                var surface = hit.collider.GetComponent<FootstepSurface>();
                if (surface != null)
                {
                    AudioClip clip = surface.GetRandomClip();
                    if (clip != null)
                    {
                        _audioSource.PlayOneShot(clip, surface.VolumeScale);
                        return;
                    }
                }
            }

            // Fallback: mặt đất không có FootstepSurface
            PlayDefaultFootstep();
        }

        private void PlayDefaultFootstep()
        {
            if (_defaultClips == null || _defaultClips.Length == 0) return;
            AudioClip clip = _defaultClips[Random.Range(0, _defaultClips.Length)];
            if (clip == null) return;
            _audioSource.PlayOneShot(clip, _defaultVolume);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, Vector3.down * _rayLength);
        }
    }
}