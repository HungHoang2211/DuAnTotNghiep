using System.Collections;
using UnityEngine;
using static Weather_Controller;

public class Weather_Rain : Weather_Base
{
    [SerializeField] private GameObject _gPartRain;
    private ParticleSystem _cachedParticleSystem;

    private void Start()
    {
        clWeatherController = GetComponent<Weather_Controller>();
        if (clWeatherController != null && clWeatherController.gTimeOfDay != null)
            cachedToD = clWeatherController.gTimeOfDay.GetComponent<ToD_Base>();

        if (_gPartRain != null) _cachedParticleSystem = _gPartRain.GetComponent<ParticleSystem>();
        if (!_bUseMorningFog) _fFogMorningAmount = _fFogAmount;

        _fSoundVolumeIn = _fSoundVolume;
        _fSoundVolumeOut = 0f;

        if (_gPartRain != null && clWeatherController != null && clWeatherController.en_CurrWeather != WeatherType.RAIN)
        {
            _gPartRain.SetActive(false);
            if (_cachedParticleSystem != null) { var em = _cachedParticleSystem.emission; em.enabled = false; }
        }
    }

    private void ActivateRainSystem()
    {
        if (_gPartRain != null)
        {
            if (!_gPartRain.activeSelf) _gPartRain.SetActive(true);
            if (_cachedParticleSystem != null)
            {
                var em = _cachedParticleSystem.emission;
                if (!em.enabled) em.enabled = true;
            }
        }
    }

    public override void RunWeather()
    {
        if (clWeatherController == null || cachedToD == null) return;

        ActivateRainSystem();

        float currentFade = GetCurrentFadeTime();

        if (cachedToD.enCurrTimeset == ToD_Base.Timeset.SUNRISE)
        {
            clWeatherController.UpdateAllWeather(_fSunrise_LightIntensity, _cSunrise_LightColor, 0f, Color.black, _cSunrise_SkyTintColor, _cSunrise_SkyGroundColor, _cCloudColor, _fFogMorningAmount, _cFogColor, currentFade);
        }
        else if (cachedToD.enCurrTimeset == ToD_Base.Timeset.DAY)
        {
            clWeatherController.UpdateAllWeather(_fDay_LightIntensity, _cDay_LightColor, 0f, Color.black, _cDay_SkyTintColor, _cDay_SkyGroundColor, _cCloudColor, _fFogAmount, _cFogColor, currentFade);
        }
        else if (cachedToD.enCurrTimeset == ToD_Base.Timeset.SUNSET)
        {
            clWeatherController.UpdateAllWeather(_fSunset_LightIntensity, _cSunset_LightColor, 0f, Color.black, _cSunset_SkyTintColor, _cSunset_SkyGroundColor, _cCloudColor, _fFogAmount, _cFogColor, currentFade);
        }
        else if (cachedToD.enCurrTimeset == ToD_Base.Timeset.NIGHT)
        {
            clWeatherController.UpdateAllWeather(_fNight_LightIntensity, _cNight_LightColor, 0f, Color.black, _cNight_SkyTintColor, _cNight_SkyGroundColor, _cCloudColor, _fFogAmount, _cFogColor, currentFade);
        }
    }

    public override void ExitWeatherEffect(GameObject gameobject, float progress)
    {
        if (clWeatherController != null && clWeatherController.en_NewWeather == WeatherType.RAIN)
            return;

        base.ExitWeatherEffect(gameobject, progress);

        if (_cachedParticleSystem != null)
        {
            var em = _cachedParticleSystem.emission;
            if (em.enabled) em.enabled = false;
        }

        if (_gPartRain != null && progress >= 1.0f)
        {
            _gPartRain.SetActive(false);
        }
    }
}