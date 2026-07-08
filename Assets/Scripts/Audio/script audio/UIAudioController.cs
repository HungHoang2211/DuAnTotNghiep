using UnityEngine;

namespace SimpleSurvival.Audio
{
    public class UIAudioController : MonoBehaviour
    {
        [Header("UI Cues")]
        [SerializeField] private AudioCue clickCue;
        [SerializeField] private AudioCue confirmCue;
        [SerializeField] private AudioCue cancelCue;
        [SerializeField] private AudioCue errorCue;

        public void PlayClick()
        {
            AudioManager.Instance.PlaySfx(clickCue);
        }

        public void PlayConfirm()
        {
            AudioManager.Instance.PlaySfx(confirmCue);
        }

        public void PlayCancel()
        {
            AudioManager.Instance.PlaySfx(cancelCue);
        }

        public void PlayError()
        {
            AudioManager.Instance.PlaySfx(errorCue);
        }
    }
}
