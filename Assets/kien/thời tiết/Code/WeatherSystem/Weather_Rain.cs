using System.Collections;
using UnityEngine;
using static Weather_Controller;

public class Weather_Rain : Weather_Base
{
    [SerializeField] private GameObject _gPartRain;
    private float _fEndParticleTimerStart;
    private float _fEndParticleTimerEnd = 5.0f;
    private ParticleSystem _cachedParticleSystem;
    private bool _bIsExitingWeather = false;

    private void Start()
    {
        clWeatherController = GetComponent<Weather_Controller>();
        if (_gPartRain != null) _cachedParticleSystem = _gPartRain.GetComponent<ParticleSystem>();
        if (!_bUseMorningFog) _fFogMorningAmount = _fFogAmount;

        _fSoundVolumeIn = _fSoundVolume;
        _fSoundVolumeOut = 0f;
        _fEndParticleTimerStart = 0.0f;

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
        if (clWeatherController == null || clWeatherController.gTimeOfDay == null) return;
        ToD_Base tod = clWeatherController.gTimeOfDay.GetComponent<ToD_Base>();

        _bIsExitingWeather = false;
        _fEndParticleTimerStart = 0.0f;

        ActivateRainSystem();

        float currentFade = _fFadeTime;

        if (tod.enCurrTimeset == ToD_Base.Timeset.SUNRISE)
        {
            clWeatherController.UpdateAllWeather(_fSunrise_LightIntensity, _cSunrise_LightColor, 0f, Color.black, _cSunrise_SkyTintColor, _cSunrise_SkyGroundColor, _cCloudColor, _fFogMorningAmount, _cFogColor, currentFade);
        }
        else if (tod.enCurrTimeset == ToD_Base.Timeset.DAY)
        {
            clWeatherController.UpdateAllWeather(_fDay_LightIntensity, _cDay_LightColor, 0f, Color.black, _cDay_SkyTintColor, _cDay_SkyGroundColor, _cCloudColor, _fFogAmount, _cFogColor, currentFade);
        }
        else if (tod.enCurrTimeset == ToD_Base.Timeset.SUNSET)
        {
            clWeatherController.UpdateAllWeather(_fSunset_LightIntensity, _cSunset_LightColor, 0f, Color.black, _cSunset_SkyTintColor, _cSunset_SkyGroundColor, _cCloudColor, _fFogAmount, _cFogColor, currentFade);
        }
        else if (tod.enCurrTimeset == ToD_Base.Timeset.NIGHT)
        {
            clWeatherController.UpdateAllWeather(_fNight_LightIntensity, _cNight_LightColor, 0f, Color.black, _cNight_SkyTintColor, _cNight_SkyGroundColor, _cCloudColor, _fFogAmount, _cFogColor, currentFade);
        }
    }

    public override void ForceWeatherChange()
    {
        ActivateRainSystem();

        if (_gPartRain == null)
            Debug.LogError("We are missing rain particles on: " + this.gameObject + " For weather type: RAIN");
    }

    public override void TurnOnSound(GameObject gameobject)
    {
        base.TurnOnSound(gameobject);
        _bTurnOffSoundAtExit = true;
    }

    public override void ExitWeatherEffect(GameObject gameobject)
    {
        // SỬA LỖI CHÍ MẠNG: Chỉ chặn tắt mưa nếu thời tiết "sắp tới" cũng là RAIN. 
        // Nếu en_NewWeather đang hướng tới SUN (1), chúng ta phải cho phép chạy xuống dưới để tắt hạt mưa!
        if (clWeatherController != null && clWeatherController.en_NewWeather == WeatherType.RAIN)
        {
            _bIsExitingWeather = false;
            return;
        }

        if (!_bIsExitingWeather)
        {
            _bIsExitingWeather = true;
            _fEndParticleTimerStart = 0.0f;

            if (_bTurnOffSoundAtExit)
            {
                base.ExitWeatherEffect(gameobject);
                _bTurnOffSoundAtExit = false;
            }

            if (_cachedParticleSystem != null)
            {
                var em = _cachedParticleSystem.emission;
                em.enabled = false; // Tắt phun hạt mới để hạt cũ rơi nốt
            }
        }

        // Tiến trình đếm ngược tắt hẳn object mưa
        if (_bIsExitingWeather && _gPartRain != null && _gPartRain.activeSelf)
        {
            _fEndParticleTimerStart += Time.deltaTime;
            if (_fEndParticleTimerStart >= _fEndParticleTimerEnd)
            {
                _gPartRain.SetActive(false);
                _bIsExitingWeather = false;
                _fEndParticleTimerStart = 0.0f;
            }
        }
    }
}