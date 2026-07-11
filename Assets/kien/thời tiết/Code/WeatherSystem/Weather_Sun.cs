using UnityEngine;
using System.Collections;

public class Weather_Sun : Weather_Base
{
    [SerializeField] protected GameObject _gSoundEffect;

    private void Start()
    {
        clWeatherController = GetComponent<Weather_Controller>();
        if (clWeatherController != null && clWeatherController.gTimeOfDay != null)
            cachedToD = clWeatherController.gTimeOfDay.GetComponent<ToD_Base>();

        if (!_bUseMorningFog) _fFogMorningAmount = _fFogAmount;

        _fSoundVolumeIn = _fSoundVolume;
        _fSoundVolumeOut = 0f;
    }

    public override void RunWeather()
    {
        if (clWeatherController == null || cachedToD == null) return;

        float currentFade = GetCurrentFadeTime();

        if (cachedToD.enCurrTimeset == ToD_Base.Timeset.SUNRISE)
        {
            float fog = _bUseMorningFog ? _fFogMorningAmount : _fFogAmount;
            clWeatherController.UpdateAllWeather(_fSunrise_LightIntensity, _cSunrise_LightColor, 0f, Color.black, _cSunrise_SkyTintColor, _cSunrise_SkyGroundColor, _cCloudColor, fog, _cFogColor, currentFade);
            clWeatherController.DeactivateTimesetParticle(_pNightParticle);
            clWeatherController.ActivateTimesetParticle(_pSunriseParticle);
        }
        else if (cachedToD.enCurrTimeset == ToD_Base.Timeset.DAY)
        {
            clWeatherController.UpdateAllWeather(_fDay_LightIntensity, _cDay_LightColor, 0f, Color.black, _cDay_SkyTintColor, _cDay_SkyGroundColor, _cCloudColor, _fFogAmount, _cFogColor, currentFade);
            clWeatherController.DeactivateTimesetParticle(_pSunriseParticle);
            clWeatherController.ActivateTimesetParticle(_pDayParticle);
        }
        else if (cachedToD.enCurrTimeset == ToD_Base.Timeset.SUNSET)
        {
            clWeatherController.UpdateAllWeather(_fSunset_LightIntensity, _cSunset_LightColor, 0f, Color.black, _cSunset_SkyTintColor, _cSunset_SkyGroundColor, _cCloudColor, _fFogAmount, _cFogColor, currentFade);
            clWeatherController.DeactivateTimesetParticle(_pDayParticle);
            clWeatherController.ActivateTimesetParticle(_pSunsetParticle);
        }
        else if (cachedToD.enCurrTimeset == ToD_Base.Timeset.NIGHT)
        {
            clWeatherController.UpdateAllWeather(_fNight_LightIntensity, _cNight_LightColor, 0f, Color.black, _cNight_SkyTintColor, _cNight_SkyGroundColor, _cCloudColor, _fFogAmount, _cFogColor, currentFade);
            clWeatherController.DeactivateTimesetParticle(_pSunsetParticle);
            clWeatherController.ActivateTimesetParticle(_pNightParticle);
        }
    }

    public override void ExitWeatherEffect(GameObject gameobject, float progress)
    {
        clWeatherController.DeactivateTimesetParticle(_pSunriseParticle);
        clWeatherController.DeactivateTimesetParticle(_pDayParticle);
        clWeatherController.DeactivateTimesetParticle(_pSunsetParticle);
        clWeatherController.DeactivateTimesetParticle(_pNightParticle);

        base.ExitWeatherEffect(gameobject, progress);
    }
}