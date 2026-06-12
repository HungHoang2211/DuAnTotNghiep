using UnityEngine;
using System.Collections;

public class Weather_Snow : Weather_Base
{
    /********** ----- VARIABLES ----- **********/

    /// <summary>
    /// Here we save the particle effect that we use for the snow effect. This is also where the sound will play from\n
    /// *Use \link GetSet_gPartSnow \endlink if you want to access this.
    /// </summary>
    [SerializeField]
    private GameObject _gPartSnow;

    private float _fEndParticleTimerStart;
    private float _fEndParticleTimerEnd;

    // Tối ưu hiệu suất: Lưu cache thành phần ParticleSystem để tránh gọi GetComponent liên tục
    private ParticleSystem _cachedParticleSystem;

    /********** ----- GETTERS AND SETTERS ----- **********/

    public GameObject GetSet_gPartSnow
    {
        get { return _gPartSnow; }
        set { _gPartSnow = value; }
    }


    private void Start()
    {
        clWeatherController = (Weather_Controller)this.GetComponent(typeof(Weather_Controller));

        // Khởi tạo cache ParticleSystem từ đầu game
        if (_gPartSnow != null)
        {
            _cachedParticleSystem = _gPartSnow.GetComponent<ParticleSystem>();
        }

        if (_bUseMorningFog == false)
            _fFogMorningAmount = _fFogAmount;

        // Make sure we fade the sound in and out
        _fSoundVolumeIn = _fSoundVolume;
        _fSoundVolumeOut = _fSoundVolume;

        // This timer makes sure that the rain stops falling and don't suddenly just disappears
        _fEndParticleTimerStart = 0.0f;
        _fEndParticleTimerEnd = 5.0f;

        if (_bUsingSound == true && _gPartSnow != null)
        {
            if (_adAmbientSound != null)
            {
                AudioSource snowAudio = _gPartSnow.GetComponent<AudioSource>();
                if (snowAudio != null)
                {
                    _bGotAudioSource = true;
                    snowAudio.clip = _adAmbientSound;
                    snowAudio.volume = 0.0f;
                    snowAudio.loop = true;
                }
                else
                {
                    snowAudio = _gPartSnow.AddComponent<AudioSource>();
                    snowAudio.clip = _adAmbientSound;
                    snowAudio.volume = 0.0f;
                    snowAudio.loop = true;
                    Debug.LogWarning("There was no AUDIOSOURCE on " + _gPartSnow + " this is now added");
                    _bGotAudioSource = true;
                }
            }
            else
                Debug.Log("There is no AMBIENT SOUND attached to the WeatherController on type: " + clWeatherController.en_CurrWeather + " If you don't want to use Ambient sound on this weather type, set Using Ambient Sound to false!");
        }
    }

    public override void Init()
    {
        base.Init();
        TurnOnSnow();

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

    private void TurnOnSnow()
    {
        if (_gPartSnow != null)
        {
            if (_gPartSnow.activeInHierarchy == false)
            {
                _gPartSnow.SetActive(true);

                if (_bUsingSound == true)
                    TurnOnSound(_gPartSnow);
            }
        }
        else
            Debug.LogError("We are missing snow particles on: " + this.gameObject + " For weather type: SNOW");
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

        // SỬA LỖI LOGIC: Đảo thứ tự check Null lên trước tiên để tránh lỗi crash hệ thống nửa chừng
        if (_gPartSnow != null && _gPartSnow.activeInHierarchy == true)
        {
            _fEndParticleTimerStart += Time.deltaTime;

            // SỬA LỖI: Thay thế thuộc tính enableEmission đã bị Unity khai tử bằng module emission mới thông qua cache
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
                _gPartSnow.SetActive(false);
            }
        }
    }
}