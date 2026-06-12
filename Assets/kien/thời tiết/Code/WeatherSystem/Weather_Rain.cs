using UnityEngine;
using System.Collections;

public class Weather_Rain : Weather_Base
{
    /********** ----- VARIABLES ----- **********/

    [SerializeField]
    private GameObject _gPartRain;

    private float _fEndParticleTimerStart;
    private float _fEndParticleTimerEnd;

    // Tối ưu hiệu suất: Lưu cache thành phần ParticleSystem để tránh gọi GetComponent liên tục trong Update/Exit
    private ParticleSystem _cachedParticleSystem;

    /********** ----- GETTERS AND SETTERS ----- **********/

    public GameObject GetSet_gPartRain
    {
        get { return _gPartRain; }
        set { _gPartRain = value; }
    }

    private void Start()
    {
        clWeatherController = (Weather_Controller)this.GetComponent(typeof(Weather_Controller));

        // Khởi tạo cache ParticleSystem từ đầu
        if (_gPartRain != null)
        {
            _cachedParticleSystem = _gPartRain.GetComponent<ParticleSystem>();
        }

        if (_bUseMorningFog == false)
            _fFogMorningAmount = _fFogAmount;

        _fSoundVolumeIn = _fSoundVolume;
        _fSoundVolumeOut = _fSoundVolume;

        _fEndParticleTimerStart = 0.0f;
        _fEndParticleTimerEnd = 5.0f;

        if (_bUsingSound == true && _gPartRain != null)
        {
            if (_adAmbientSound != null)
            {
                AudioSource rainAudio = _gPartRain.GetComponent<AudioSource>();
                if (rainAudio != null)
                {
                    _bGotAudioSource = true;
                    rainAudio.clip = _adAmbientSound;
                    rainAudio.volume = 0.0f;
                    rainAudio.loop = true;
                }
                else
                {
                    rainAudio = _gPartRain.AddComponent<AudioSource>();
                    rainAudio.clip = _adAmbientSound;
                    rainAudio.volume = 0.0f;
                    rainAudio.loop = true;
                    Debug.LogWarning("There was no AUDIOSOURCE on " + _gPartRain + " this is now added");

                    _bGotAudioSource = true;
                }
            }
            else
                Debug.Log("There is no AMBIENT SOUND attached to the WeatherController on type: " + clWeatherController.en_CurrWeather);
        }
    }

    public override void Init()
    {
        base.Init();
        TurnOnRain();

        // SỬA LỖI: Cập nhật sang cú pháp Unity mới cho module Emission
        if (_cachedParticleSystem != null)
        {
            var emission = _cachedParticleSystem.emission;
            emission.enabled = true;
        }
    }

    private void Update()
    {
        UpdateWeather();

        if (_bUseInit == true)
        {
            _fInitTimerStart += Time.deltaTime;

            if (_fInitTimerStart >= _fInitTimerEnd)
            {
                Init();
                _fInitTimerStart = 0.0f;
                _bUseInit = false;
            }
        }
    }

    public override void UpdateWeather()
    {
        if (_bUseDifferentFadeTimes == false)
            OneFadeTimeToRuleThemAll();
        else
            DifferentFadeTimes();
    }

    private void OneFadeTimeToRuleThemAll()
    {
        var tod = clWeatherController.gTimeOfDay.GetComponent<ToD_Base>();
        if (tod.enCurrTimeset == ToD_Base.Timeset.SUNRISE)
        {
            clWeatherController.UpdateAllWeather(_fSunrise_LightIntensity, _cSunrise_LightColor, 0.0f, _cNight_MoonLightColor,
                _cSunrise_SkyTintColor, _cSunrise_SkyGroundColor, _cCloudColor, _fFogMorningAmount, _cFogColor, _fFadeTime);

            clWeatherController.DeactivateTimesetParticle(_pNightParticle);
            clWeatherController.ActivateTimesetParticle(_pSunriseParticle);
        }
        else if (tod.enCurrTimeset == ToD_Base.Timeset.DAY)
        {
            clWeatherController.UpdateAllWeather(_fDay_LightIntensity, _cDay_LightColor, 0.0f, _cNight_MoonLightColor,
                _cDay_SkyTintColor, _cDay_SkyGroundColor, _cCloudColor, _fFogAmount, _cFogColor, _fFadeTime);

            clWeatherController.DeactivateTimesetParticle(_pSunriseParticle);
            clWeatherController.ActivateTimesetParticle(_pDayParticle);
        }
        else if (tod.enCurrTimeset == ToD_Base.Timeset.SUNSET)
        {
            clWeatherController.UpdateAllWeather(_fSunset_LightIntensity, _cSunset_LightColor, 0.0f, _cNight_MoonLightColor,
                _cSunset_SkyTintColor, _cSunset_SkyGroundColor, _cCloudColor, _fFogAmount, _cFogColor, _fFadeTime);

            clWeatherController.DeactivateTimesetParticle(_pDayParticle);
            clWeatherController.ActivateTimesetParticle(_pSunsetParticle);
        }
        else if (tod.enCurrTimeset == ToD_Base.Timeset.NIGHT)
        {
            clWeatherController.UpdateAllWeather(_fNight_LightIntensity, _cNight_LightColor, _fNight_MoonLightIntensity,
                _cNight_MoonLightColor, _cNight_SkyTintColor, _cNight_SkyGroundColor, _cCloudColor, _fFogAmount, _cFogColor, _fFadeTime);

            clWeatherController.DeactivateTimesetParticle(_pSunsetParticle);
            clWeatherController.ActivateTimesetParticle(_pNightParticle);
        }
    }

    private void DifferentFadeTimes()
    {
        var tod = clWeatherController.gTimeOfDay.GetComponent<ToD_Base>();
        if (tod.enCurrTimeset == ToD_Base.Timeset.SUNRISE)
        {
            clWeatherController.UpdateAllWeather(_fSunrise_LightIntensity, _cSunrise_LightColor, 0.0f, _cNight_MoonLightColor,
                _cSunrise_SkyTintColor, _cSunrise_SkyGroundColor, _cCloudColor, _fFogMorningAmount, _cFogColor, _fSunriseFadeTime);

            clWeatherController.DeactivateTimesetParticle(_pNightParticle);
            clWeatherController.ActivateTimesetParticle(_pSunriseParticle);
        }
        else if (tod.enCurrTimeset == ToD_Base.Timeset.DAY)
        {
            clWeatherController.UpdateAllWeather(_fDay_LightIntensity, _cDay_LightColor, 0.0f, _cNight_MoonLightColor,
                _cDay_SkyTintColor, _cDay_SkyGroundColor, _cCloudColor, _fFogAmount, _cFogColor, _fDayFadeTime);

            clWeatherController.DeactivateTimesetParticle(_pSunriseParticle);
            clWeatherController.ActivateTimesetParticle(_pDayParticle);
        }
        else if (tod.enCurrTimeset == ToD_Base.Timeset.SUNSET)
        {
            clWeatherController.UpdateAllWeather(_fSunset_LightIntensity, _cSunset_LightColor, 0.0f, _cNight_MoonLightColor,
                _cSunset_SkyTintColor, _cSunset_SkyGroundColor, _cCloudColor, _fFogAmount, _cFogColor, _fSunsetFadeTime);

            clWeatherController.DeactivateTimesetParticle(_pDayParticle);
            clWeatherController.ActivateTimesetParticle(_pSunsetParticle);
        }
        else if (tod.enCurrTimeset == ToD_Base.Timeset.NIGHT)
        {
            clWeatherController.UpdateAllWeather(_fNight_LightIntensity, _cNight_LightColor, _fNight_MoonLightIntensity, _cNight_MoonLightColor,
                _cNight_SkyTintColor, _cNight_SkyGroundColor, _cCloudColor, _fFogAmount, _cFogColor, _fNightFadeTime);

            clWeatherController.DeactivateTimesetParticle(_pSunsetParticle);
            clWeatherController.ActivateTimesetParticle(_pNightParticle);
        }
    }

    private void TurnOnRain()
    {
        if (_gPartRain != null)
        {
            if (_gPartRain.activeInHierarchy == false)
            {
                _gPartRain.SetActive(true);

                if (_bUsingSound == true)
                    TurnOnSound(_gPartRain);
            }
        }
        else
            Debug.LogError("We are missing rain particles on: " + this.gameObject + " For weather type: RAIN");
    }

    public override void TurnOnSound(GameObject gameobject)
    {
        base.TurnOnSound(gameobject);
        _bTurnOffSoundAtExit = true;
    }

    public override void ExitWeatherEffect(GameObject gameobject)
    {
        clWeatherController.DeactivateTimesetParticle(_pSunriseParticle);
        clWeatherController.DeactivateTimesetParticle(_pDayParticle);
        clWeatherController.DeactivateTimesetParticle(_pSunsetParticle);
        clWeatherController.DeactivateTimesetParticle(_pNightParticle);

        if (_gPartRain != null && _gPartRain.activeInHierarchy == true)
        {
            _fEndParticleTimerStart += Time.deltaTime;

            // SỬA LỖI: Thay đổi enableEmission lỗi thời bằng cách gọi trực tiếp qua thuộc tính cached mới
            if (_cachedParticleSystem != null)
            {
                var emission = _cachedParticleSystem.emission;
                emission.enabled = false;
            }

            if (_bTurnOffSoundAtExit == true)
            {
                if (_bUsingSound == true)
                    base.ExitWeatherEffect(gameobject);

                _bTurnOffSoundAtExit = false;
            }

            if (_fEndParticleTimerStart > _fEndParticleTimerEnd)
            {
                _fEndParticleTimerStart = 0.0f;
                _gPartRain.SetActive(false);
            }
        }
    }
}