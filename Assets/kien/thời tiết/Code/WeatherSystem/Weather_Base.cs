using UnityEngine;
using System.Collections;

public class Weather_Base : MonoBehaviour
{
    protected Weather_Controller clWeatherController;
    protected ToD_Base cachedToD;

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
    [SerializeField] protected AudioSource _asAmbientSource;

    protected float _fSoundVolumeIn;
    protected float _fSoundVolumeOut = 0.0f;
    protected bool _bGotAudioSource = false;

    [Header("Weather Modifiers (nhan len Environment baseline cua ToD_Base)")]
    [SerializeField] protected float _fLightIntensityMultiplier = 1.0f;
    [SerializeField] protected Color _cLightColorTint = Color.white;
    [SerializeField] protected Color _cSkyTintMultiply = Color.white;
    [SerializeField] protected Color _cSkyGroundMultiply = Color.white;
    [SerializeField] protected float _fFogAmountMultiplier = 1.0f;
    [SerializeField] protected Color _cFogColorTint = Color.white;

    [Header("Morning Fog Override")]
    [SerializeField] protected bool _bUseMorningFog;
    [SerializeField] protected float _fFogMorningMultiplier = 1.0f;

    [Header("Shared Material Settings")]
    [SerializeField] protected Color _cCloudColor = Color.white;

    [Header("Particles (Optional)")]
    [SerializeField] protected GameObject _pSunriseParticle;
    [SerializeField] protected GameObject _pDayParticle;
    [SerializeField] protected GameObject _pSunsetParticle;
    [SerializeField] protected GameObject _pNightParticle;

    public virtual void RunWeather() { }

    public virtual float GetCurrentFadeTime()
    {
        if (_bUseDifferentFadeTimes && cachedToD != null)
        {
            switch (cachedToD.enCurrTimeset)
            {
                case ToD_Base.Timeset.SUNRISE: return _fSunriseFadeTime;
                case ToD_Base.Timeset.DAY: return _fDayFadeTime;
                case ToD_Base.Timeset.SUNSET: return _fSunsetFadeTime;
                case ToD_Base.Timeset.NIGHT: return _fNightFadeTime;
            }
        }
        return _fFadeTime;
    }

    protected void GetEnvironmentTarget(ToD_Base.Timeset timeset, out float lightIntensity, out Color lightColor, out float moonIntensity, out Color moonColor, out Color skyTint, out Color skyGround, out float fogAmount, out Color fogColor)
    {
        ToD_EnvironmentSettings baseEnv = cachedToD.GetEnvironmentSettings(timeset);

        lightIntensity = baseEnv.LightIntensity * _fLightIntensityMultiplier;
        lightColor = baseEnv.LightColor * _cLightColorTint;
        moonIntensity = baseEnv.MoonIntensity;
        moonColor = baseEnv.MoonColor;
        skyTint = baseEnv.SkyTintColor * _cSkyTintMultiply;
        skyGround = baseEnv.SkyGroundColor * _cSkyGroundMultiply;

        float morningMultiplier = (_bUseMorningFog && timeset == ToD_Base.Timeset.SUNRISE) ? _fFogMorningMultiplier : 1.0f;
        fogAmount = baseEnv.FogAmount * _fFogAmountMultiplier * morningMultiplier;
        fogColor = baseEnv.FogColor * _cFogColorTint;
    }

    public virtual void TurnOnSound(GameObject gameobject)
    {
        if (!_bUsingSound) return;

        GameObject soundHost = _asAmbientSource != null ? _asAmbientSource.gameObject : gameobject;
        AudioSource audio = _asAmbientSource != null ? _asAmbientSource : soundHost.GetComponent<AudioSource>();

        if (audio != null && _adAmbientSound != null)
        {
            _bGotAudioSource = true;
            audio.clip = _adAmbientSound;
            audio.volume = _fSoundVolumeOut;

            if (soundHost.GetComponent<Weather_SoundFade>() == null)
            {
                soundHost.AddComponent<Weather_SoundFade>();
            }
            soundHost.GetComponent<Weather_SoundFade>().FadeAudioIn(_fTimeToFadeSound, _fSoundVolumeIn);
            audio.Play();
        }
    }

    public virtual void ExitWeatherEffect(GameObject gameobject, float progress)
    {
        if (!_bUsingSound || !_bGotAudioSource) return;

        AudioSource audio = _asAmbientSource != null ? _asAmbientSource : gameobject.GetComponent<AudioSource>();
        if (audio == null) return;

        float p = Mathf.Clamp01(progress);
        audio.volume = Mathf.Lerp(_fSoundVolumeIn, _fSoundVolumeOut, p);

        if (p >= 1.0f)
        {
            audio.Stop();
            _bGotAudioSource = false;
        }
    }
}