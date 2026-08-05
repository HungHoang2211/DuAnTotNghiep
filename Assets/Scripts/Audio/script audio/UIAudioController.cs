using UnityEngine;

namespace SimpleSurvival.Audio
{
    public class UIAudioController : MonoBehaviour
    {
        public static UIAudioController Instance { get; private set; }

        [Header("UI Cues")]
        [SerializeField] private AudioCue clickCue;
        [SerializeField] private AudioCue okcue;
        [SerializeField] private AudioCue cancelCue;
        [SerializeField] private AudioCue errorCue;
        [SerializeField] private AudioCue itemMoveCue;
        [SerializeField] private AudioCue useItemCue;
        [SerializeField] private AudioCue deleteCue;
        [SerializeField] private AudioCue craft ;
        [SerializeField] private AudioCue mainClickCue ;
        [SerializeField] private AudioCue updatecue ;
        public void PlayMainClick()
        {
            AudioManager.Instance.PlaySfx(mainClickCue);
        }

        private void Awake()
        {
            Instance = this;
        }

        public void PlayClick()
        {
            AudioManager.Instance.PlaySfx(clickCue);
        }
        public void Playcraft()
        {
            AudioManager.Instance.PlaySfx(craft);
        }

        public void Playoke()
        {
            AudioManager.Instance.PlaySfx(okcue);
        }
        public void Playupdate()
        {
            AudioManager.Instance.PlaySfx(updatecue);
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