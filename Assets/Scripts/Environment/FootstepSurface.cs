using UnityEngine;

namespace Xyla.Audio
{
    /// <summary>
    /// Gắn lên mặt đất (Grass, Wood, Stone...).
    /// Chứa bộ clip footstep tương ứng với loại mặt đất đó.
    /// </summary>
    public class FootstepSurface : MonoBehaviour
    {
        [Tooltip("Tên mặt đất để debug (Grass, Wood, Stone...).")]
        [SerializeField] private string _surfaceName = "Default";

        [Tooltip("Các clip footstep của mặt đất này. Nhiều clip = random không bị lặp.")]
        [SerializeField] private AudioClip[] _clips;

        [Tooltip("Âm lượng riêng của mặt đất này (cỏ nhẹ hơn gỗ).")]
        [SerializeField][Range(0f, 1f)] private float _volumeScale = 1f;

        public string SurfaceName => _surfaceName;
        public float VolumeScale => _volumeScale;

        /// <summary>Lấy random 1 clip. Trả null nếu chưa setup.</summary>
        public AudioClip GetRandomClip()
        {
            if (_clips == null || _clips.Length == 0) return null;
            return _clips[Random.Range(0, _clips.Length)];
        }
    }
}