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

        GetEnvironmentTarget(cachedToD.enCurrTimeset, out float lightIntensity, out Color lightColor, out float moonIntensity, out Color moonColor, out Color skyTint, out Color skyGround, out float fogAmount, out Color fogColor);

        float currentFade = GetCurrentFadeTime();
        clWeatherController.UpdateAllWeather(lightIntensity, lightColor, moonIntensity, moonColor, skyTint, skyGround, _cCloudColor, fogAmount, fogColor, currentFade);
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