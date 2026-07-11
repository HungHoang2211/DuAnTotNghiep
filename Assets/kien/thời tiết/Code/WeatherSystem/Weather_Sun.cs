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

        _fSoundVolumeIn = _fSoundVolume;
        _fSoundVolumeOut = 0f;
    }

    public override void RunWeather()
    {
        if (clWeatherController == null || cachedToD == null) return;

        GetEnvironmentTarget(cachedToD.enCurrTimeset, out float lightIntensity, out Color lightColor, out float moonIntensity, out Color moonColor, out Color skyTint, out Color skyGround, out float fogAmount, out Color fogColor);

        float currentFade = GetCurrentFadeTime();
        clWeatherController.UpdateAllWeather(lightIntensity, lightColor, moonIntensity, moonColor, skyTint, skyGround, _cCloudColor, fogAmount, fogColor, currentFade);

        switch (cachedToD.enCurrTimeset)
        {
            case ToD_Base.Timeset.SUNRISE:
                clWeatherController.DeactivateTimesetParticle(_pNightParticle);
                clWeatherController.ActivateTimesetParticle(_pSunriseParticle);
                break;
            case ToD_Base.Timeset.DAY:
                clWeatherController.DeactivateTimesetParticle(_pSunriseParticle);
                clWeatherController.ActivateTimesetParticle(_pDayParticle);
                break;
            case ToD_Base.Timeset.SUNSET:
                clWeatherController.DeactivateTimesetParticle(_pDayParticle);
                clWeatherController.ActivateTimesetParticle(_pSunsetParticle);
                break;
            case ToD_Base.Timeset.NIGHT:
                clWeatherController.DeactivateTimesetParticle(_pSunsetParticle);
                clWeatherController.ActivateTimesetParticle(_pNightParticle);
                break;
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