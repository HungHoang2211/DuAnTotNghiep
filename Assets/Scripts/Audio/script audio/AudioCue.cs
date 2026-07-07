using UnityEngine;

namespace SimpleSurvival.Audio
{
    [CreateAssetMenu(fileName = "AudioCue", menuName = "SimpleSurvival/Audio/Audio Cue")]
    public class AudioCue : ScriptableObject
    {
        [SerializeField] private AudioClip[] clips;
        [SerializeField] private AudioCategory category = AudioCategory.Sfx;

        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField, Range(0.1f, 3f)] private float minPitch = 1f;
        [SerializeField, Range(0.1f, 3f)] private float maxPitch = 1f;

        [SerializeField, Range(0f, 1f)] private float spatialBlend;
        [SerializeField] private float minDistance = 1f;
        [SerializeField] private float maxDistance = 25f;

        [SerializeField, Range(0, 256)] private int priority = 128;

        public AudioCategory Category => category;
        public float Volume => volume;
        public float SpatialBlend => spatialBlend;
        public float MinDistance => minDistance;
        public float MaxDistance => maxDistance;
        public int Priority => priority;

        public bool HasClip => clips != null && clips.Length > 0;

        public AudioClip PickClip()
        {
            if (!HasClip)
                return null;

            return clips[Random.Range(0, clips.Length)];
        }

        public float PickPitch()
        {
            return Random.Range(minPitch, maxPitch);
        }
    }
}
