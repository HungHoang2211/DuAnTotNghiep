using UnityEngine;
using UnityEngine.UI;

namespace SimpleSurvival.Audio
{
    public class AudioSettingsUI : MonoBehaviour
    {
        [Header("Sliders")]
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Slider uiSlider;
        [SerializeField] private Slider ambienceSlider;

        [Header("Pause Game While Panel Open")]
        [Tooltip(
            "Khi Settings mở: gameplay pause + gameplay audio pause. " +
            "Music và ambience vẫn hoạt động."
        )]
        [SerializeField] private bool pauseGameWhileOpen = true;

        private void OnEnable()
        {
            if (!pauseGameWhileOpen)
                return;

            // Pause gameplay
            Time.timeScale = 0f;

            // Pause gameplay audio
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PauseGameplayAudio();
            }
        }

        private void OnDisable()
        {
            if (!pauseGameWhileOpen)
                return;

            // Resume gameplay
            Time.timeScale = 1f;

            // Resume gameplay audio
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.ResumeGameplayAudio();
            }
        }

        private void Start()
        {
            if (AudioManager.Instance == null)
            {
                Debug.LogWarning(
                    "[AudioSettingsUI] AudioManager not found!"
                );

                return;
            }

            // =====================================================
            // LOAD VALUES
            // =====================================================

            if (masterSlider != null)
            {
                masterSlider.SetValueWithoutNotify(
                    AudioManager.Instance.MasterVolume
                );

                masterSlider.onValueChanged.AddListener(
                    OnMasterVolumeChanged
                );
            }

            if (sfxSlider != null)
            {
                sfxSlider.SetValueWithoutNotify(
                    AudioManager.Instance.SfxVolume
                );

                sfxSlider.onValueChanged.AddListener(
                    OnSfxVolumeChanged
                );
            }

            if (uiSlider != null)
            {
                uiSlider.SetValueWithoutNotify(
                    AudioManager.Instance.UiVolume
                );

                uiSlider.onValueChanged.AddListener(
                    OnUiVolumeChanged
                );
            }

            if (ambienceSlider != null)
            {
                ambienceSlider.SetValueWithoutNotify(
                    AudioManager.Instance.AmbienceVolume
                );

                ambienceSlider.onValueChanged.AddListener(
                    OnAmbienceVolumeChanged
                );
            }
        }


        // =========================================================
        // MASTER
        // =========================================================

        private void OnMasterVolumeChanged(
            float value)
        {
            if (AudioManager.Instance == null)
                return;

            AudioManager.Instance.SetMasterVolume(
                value
            );

            PlayerPrefs.SetFloat(
                "Audio_MasterVolume",
                value
            );

            PlayerPrefs.Save();
        }


        // =========================================================
        // SFX
        // =========================================================

        private void OnSfxVolumeChanged(
            float value)
        {
            if (AudioManager.Instance == null)
                return;

            AudioManager.Instance.SetSfxVolume(
                value
            );

            PlayerPrefs.SetFloat(
                "Audio_SfxVolume",
                value
            );

            PlayerPrefs.Save();
        }


        // =========================================================
        // UI
        // =========================================================

        private void OnUiVolumeChanged(
            float value)
        {
            if (AudioManager.Instance == null)
                return;

            AudioManager.Instance.SetUiVolume(
                value
            );

            PlayerPrefs.SetFloat(
                "Audio_UiVolume",
                value
            );

            PlayerPrefs.Save();
        }


        // =========================================================
        // AMBIENCE
        // =========================================================

        private void OnAmbienceVolumeChanged(
            float value)
        {
            if (AudioManager.Instance == null)
                return;

            AudioManager.Instance.SetAmbienceVolume(
                value
            );

            PlayerPrefs.SetFloat(
                "Audio_AmbienceVolume",
                value
            );

            PlayerPrefs.Save();
        }


        // =========================================================
        // CLEANUP
        // =========================================================

        private void OnDestroy()
        {
            if (pauseGameWhileOpen)
            {
                Time.timeScale = 1f;

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.ResumeGameplayAudio();
                }
            }

            if (masterSlider != null)
            {
                masterSlider.onValueChanged.RemoveListener(
                    OnMasterVolumeChanged
                );
            }

            if (sfxSlider != null)
            {
                sfxSlider.onValueChanged.RemoveListener(
                    OnSfxVolumeChanged
                );
            }

            if (uiSlider != null)
            {
                uiSlider.onValueChanged.RemoveListener(
                    OnUiVolumeChanged
                );
            }

            if (ambienceSlider != null)
            {
                ambienceSlider.onValueChanged.RemoveListener(
                    OnAmbienceVolumeChanged
                );
            }
        }
    }
}