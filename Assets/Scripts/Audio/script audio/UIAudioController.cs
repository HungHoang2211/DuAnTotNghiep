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
        [SerializeField] private AudioCue craft;
        [SerializeField] private AudioCue mainClickCue;
        [SerializeField] private AudioCue updatecue;

        [Header("Action Cues")]
        [SerializeField] private AudioCue pickupCue;
        [SerializeField] private AudioCue lootCue;
        [SerializeField] private AudioCue npcInteractCue;
        [SerializeField] private AudioCue witchEventCue;
        [SerializeField] private AudioCue unlockCue;

        private void Awake()
        {
            Instance = this;
        }

        // =========================================================
        // GAMEPLAY UI AUDIO
        // =========================================================

        public void PlayPickup()
        {
            if (AudioManager.Instance == null)
                return;

            AudioManager.Instance.PlayGameplaySfx(
                pickupCue
            );
        }

        public void PlayLoot()
        {
            if (AudioManager.Instance == null)
                return;

            AudioManager.Instance.PlayGameplaySfx(
                lootCue
            );
        }

        public void PlayNPCInteract()
        {
            if (AudioManager.Instance == null)
                return;

            AudioManager.Instance.PlayGameplaySfx(
                npcInteractCue
            );
        }

        public void PlayWitchEvent()
        {
            if (AudioManager.Instance == null)
                return;

            AudioManager.Instance.PlayGameplaySfx(
                witchEventCue
            );
        }

        public void PlayUnlock()
        {
            if (AudioManager.Instance == null)
                return;

            AudioManager.Instance.PlayGameplaySfx(
                unlockCue
            );
        }


        // =========================================================
        // NORMAL UI AUDIO
        // =========================================================

        public void PlayMainClick()
        {
            if (AudioManager.Instance == null)
                return;

            AudioManager.Instance.PlaySfx(
                mainClickCue
            );
        }

        public void PlayClick()
        {
            if (AudioManager.Instance == null)
                return;

            AudioManager.Instance.PlaySfx(
                clickCue
            );
        }

        public void Playcraft()
        {
            if (AudioManager.Instance == null)
                return;

            AudioManager.Instance.PlaySfx(
                craft
            );
        }

        public void Playoke()
        {
            if (AudioManager.Instance == null)
                return;

            AudioManager.Instance.PlaySfx(
                okcue
            );
        }

        public void Playupdate()
        {
            if (AudioManager.Instance == null)
                return;

            AudioManager.Instance.PlaySfx(
                updatecue
            );
        }

        public void PlayCancel()
        {
            if (AudioManager.Instance == null)
                return;

            AudioManager.Instance.PlaySfx(
                cancelCue
            );
        }

        public void PlayError()
        {
            if (AudioManager.Instance == null)
                return;

            AudioManager.Instance.PlaySfx(
                errorCue
            );
        }

        public void PlayItemMove()
        {
            if (AudioManager.Instance == null)
                return;

            AudioManager.Instance.PlaySfx(
                itemMoveCue
            );
        }

        public void PlayUseItem()
        {
            if (AudioManager.Instance == null)
                return;

            AudioManager.Instance.PlaySfx(
                useItemCue
            );
        }

        public void PlayDelete()
        {
            if (AudioManager.Instance == null)
                return;

            AudioManager.Instance.PlaySfx(
                deleteCue
            );
        }


        // =========================================================
        // UNLOCK LOOP
        // =========================================================

        public void StartUnlockSound()
        {
            if (AudioManager.Instance == null)
                return;

            AudioManager.Instance.StartGameplayLoop(
                unlockCue
            );
        }

        public void StopUnlockSound()
        {
            if (AudioManager.Instance == null)
                return;

            AudioManager.Instance.StopLoop(
                unlockCue
            );
        }


        // =========================================================
        // WITCH EVENT LOOP
        // =========================================================

        public void StartWitchEventSound()
        {
            if (AudioManager.Instance == null)
                return;

            AudioManager.Instance.StartGameplayLoop(
                witchEventCue
            );
        }

        public void StopWitchEventSound()
        {
            if (AudioManager.Instance == null)
                return;

            AudioManager.Instance.StopLoop(
                witchEventCue
            );
        }
    }
}