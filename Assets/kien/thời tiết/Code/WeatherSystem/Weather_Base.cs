using UnityEngine;
using System.Collections;

public class Weather_Base : MonoBehaviour
{
    protected Weather_Controller clWeatherController;

    [SerializeField] protected bool _bUseDifferentFadeTimes;
    [SerializeField] protected float _fFadeTime = 5.0f;
    [SerializeField] protected float _fSunriseFadeTime = 5.0f;
    [SerializeField] protected float _fDayFadeTime = 5.0f;
    [SerializeField] protected float _fSunsetFadeTime = 5.0f;
    [SerializeField] protected float _fNightFadeTime = 5.0f;

    [SerializeField] protected bool _bUsingSound;
    [SerializeField] protected float _fSoundVolume = 1.0f;
    [SerializeField] protected float _fTimeToFadeSound = 2.0f;
    [SerializeField] protected AudioClip _adAmbientSound;

    protected float _fSoundVolumeIn;
    protected float _fSoundVolumeOut = 0.0f;
    protected bool _bGotAudioSource = false;
    protected bool _bTurnOffSoundAtExit = false;

    [SerializeField] protected bool _bUseMorningFog;
    [SerializeField] protected float _fFogMorningAmount = 0.002f;
    [SerializeField] protected float _fFogAmount = 0.005f;
    [SerializeField] protected Color _cFogColor = Color.grey;

    [Header("Sunrise Settings")]
    [SerializeField] protected float _fSunrise_LightIntensity = 0.5f;
    [SerializeField] protected Color _cSunrise_LightColor = Color.white;
    [SerializeField] protected Color _cSunrise_SkyTintColor = Color.white;
    [SerializeField] protected Color _cSunrise_SkyGroundColor = Color.white;

    [Header("Day Settings")]
    [SerializeField] protected float _fDay_LightIntensity = 1.0f;
    [SerializeField] protected Color _cDay_LightColor = Color.white;
    [SerializeField] protected Color _cDay_SkyTintColor = Color.white;
    [SerializeField] protected Color _cDay_SkyGroundColor = Color.white;

    [Header("Sunset Settings")]
    [SerializeField] protected float _fSunset_LightIntensity = 0.5f;
    [SerializeField] protected Color _cSunset_LightColor = Color.white;
    [SerializeField] protected Color _cSunset_SkyTintColor = Color.white;
    [SerializeField] protected Color _cSunset_SkyGroundColor = Color.white;

    [Header("Night Settings")]
    [SerializeField] protected float _fNight_LightIntensity = 0.05f;
    [SerializeField] protected Color _cNight_LightColor = Color.blue;
    [SerializeField] protected Color _cNight_SkyTintColor = Color.black;
    [SerializeField] protected Color _cNight_SkyGroundColor = Color.black;

    [Header("Shared Material Settings")]
    [SerializeField] protected Color _cCloudColor = Color.white;

    [Header("Particles (Optional)")]
    [SerializeField] protected GameObject _pSunriseParticle;
    [SerializeField] protected GameObject _pDayParticle;
    [SerializeField] protected GameObject _pSunsetParticle;
    [SerializeField] protected GameObject _pNightParticle;

    public virtual void RunWeather() { }
    public virtual void ForceWeatherChange() { }

    public virtual void TurnOnSound(GameObject gameobject)
    {
        if (!_bUsingSound) return;

        AudioSource audio = gameobject.GetComponent<AudioSource>();
        if (audio != null && _adAmbientSound != null)
        {
            _bGotAudioSource = true;
            audio.clip = _adAmbientSound;
            if (gameobject.GetComponent<Weather_SoundFade>() == null)
            {
                gameobject.AddComponent<Weather_SoundFade>();
            }
            gameobject.GetComponent<Weather_SoundFade>().FadeAudioIn(_fTimeToFadeSound, _fSoundVolumeIn);
            audio.Play();
        }
    }

    public virtual void ExitWeatherEffect(GameObject gameobject)
    {
        if (gameobject.GetComponent<Weather_SoundFade>() != null && _bGotAudioSource)
        {
            gameobject.GetComponent<Weather_SoundFade>().FadeAudioOut(_fTimeToFadeSound, _fSoundVolumeOut);
        }
    }
}