using UnityEngine;

namespace SimpleSurvival.Audio
{
    public class MapAudioTrigger : MonoBehaviour
    {
        [SerializeField] private AudioCue musicCue;
        [SerializeField] private AudioCue ambienceCue;

        private void Start()
        {
            if (AudioManager.Instance == null) return;

            if (musicCue != null)
                AudioManager.Instance.PlayMusic(musicCue);

            if (ambienceCue != null)
                AudioManager.Instance.PlayAmbience(ambienceCue);
        }

        private void OnDestroy()
        {
            if (AudioManager.Instance == null) return;

            AudioManager.Instance.StopMusic();
        }
    }
}