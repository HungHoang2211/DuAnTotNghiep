using UnityEngine;

namespace SimpleSurvival.Audio
{
    public class HarvestableAudioController : MonoBehaviour
    {
        [SerializeField] private AudioCue impactCue;
        [SerializeField] private AudioCue depletedCue;

        private AudioSource _currentImpactSource;

        public void PlayImpact()
        {
            StopImpact();
            _currentImpactSource = AudioManager.Instance.PlaySfxAt(impactCue, transform.position);
        }

        public void StopImpact()
        {
            if (_currentImpactSource != null)
                _currentImpactSource.Stop();
        }

        public void PlayDepleted()
        {
            StopImpact();
            AudioManager.Instance.PlayImportantSfxAt(depletedCue, transform.position);
        }
    }
}