using UnityEngine;

namespace SimpleSurvival.Audio
{
    public class AudioSourceCategory : MonoBehaviour
    {
        public AudioCategory Category { get; private set; }

        public float BaseVolume { get; private set; }

        public void SetData(
            AudioCategory category,
            float baseVolume)
        {
            Category = category;
            BaseVolume = baseVolume;
        }
    }
}