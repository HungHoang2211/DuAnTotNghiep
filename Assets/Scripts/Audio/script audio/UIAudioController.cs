using UnityEngine;

namespace SimpleSurvival.Audio
{
    public class UIAudioController : MonoBehaviour
    {
        public static UIAudioController Instance { get; private set; }

        [Header("UI Cues")]
        [SerializeField] private AudioCue clickCue;
        [SerializeField] private AudioCue confirmCue;
        [SerializeField] private AudioCue cancelCue;
        [SerializeField] private AudioCue errorCue;
        [SerializeField] private AudioCue itemMoveCue;
        [SerializeField] private AudioCue useItemCue;
        [SerializeField] private AudioCue deleteCue;

        private void Awake()
        {
            Instance = this;
        }

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

        public void PlayItemMove()
        {
            AudioManager.Instance.PlaySfx(itemMoveCue);
        }

        public void PlayUseItem()
        {
            AudioManager.Instance.PlaySfx(useItemCue);
        }

        public void PlayDelete()
        {
            AudioManager.Instance.PlaySfx(deleteCue);
        }
    }
}