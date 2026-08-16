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

        private void Start()
        {
            if (AudioManager.Instance == null)
            {
                Debug.LogWarning("AudioManager not found!");
                return;
            }

            // Load giá trị hiện tại từ AudioManager
            masterSlider.value =
                AudioManager.Instance.MasterVolume;

            sfxSlider.value =
                AudioManager.Instance.SfxVolume;

            uiSlider.value =
                AudioManager.Instance.UiVolume;

            ambienceSlider.value =
                AudioManager.Instance.AmbienceVolume;

            // Đăng ký sự kiện
            masterSlider.onValueChanged.AddListener(
                OnMasterVolumeChanged
            );

            sfxSlider.onValueChanged.AddListener(
                OnSfxVolumeChanged
            );

            uiSlider.onValueChanged.AddListener(
                OnUiVolumeChanged
            );

            ambienceSlider.onValueChanged.AddListener(
                OnAmbienceVolumeChanged
            );
        }

        // =========================================================
        // MASTER
        // =========================================================

        private void OnMasterVolumeChanged(float value)
        {
            if (AudioManager.Instance == null)
                return;

            AudioManager.Instance.SetMasterVolume(value);

            PlayerPrefs.SetFloat(
                "Audio_MasterVolume",
                value
            );

            PlayerPrefs.Save();
        }

        // =========================================================
        // SFX
        // =========================================================

        private void OnSfxVolumeChanged(float value)
        {
            if (AudioManager.Instance == null)
                return;

            AudioManager.Instance.SetSfxVolume(value);

            PlayerPrefs.SetFloat(
                "Audio_SfxVolume",
                value
            );

            PlayerPrefs.Save();
        }

        // =========================================================
        // UI
        // =========================================================

        private void OnUiVolumeChanged(float value)
        {
            if (AudioManager.Instance == null)
                return;

            AudioManager.Instance.SetUiVolume(value);

            PlayerPrefs.SetFloat(
                "Audio_UiVolume",
                value
            );

            PlayerPrefs.Save();
        }

        // =========================================================
        // AMBIENCE
        // =========================================================

        private void OnAmbienceVolumeChanged(float value)
        {
            if (AudioManager.Instance == null)
                return;

            AudioManager.Instance.SetAmbienceVolume(value);

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
            if (AudioManager.Instance == null)
                return;

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