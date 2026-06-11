using UnityEngine;
using System.Collections;

public class Weather_Thunderstorm : Weather_Base
{
    /********** ----- VARIABLES ----- **********/

    /********** GAMEOBJECTS Settings **********/
    [SerializeField]
    private GameObject _gPartRain;

    private GameObject _gLighting;
    private ParticleSystem _cachedRainParticleSystem; // Tối ưu: Lưu cache Particle để tránh GetComponent liên tục
    private Light _lightingComponent;                 // Tối ưu: Lưu cache Light component
    private AudioSource _lightingAudioSource;         // Tối ưu: Lưu cache AudioSource component

    /********** GENERAL Settings **********/
    private float _fEndParticleTimerStart;
    private float _fEndParticleTimerEnd;

    /********** THUNDER Settings **********/
    private float _fTimerForNextLighting;
    private float _fTimeForNextLighting;

    [SerializeField] private float _fRandomThunderTimeMin = 30.0f;
    [SerializeField] private float _fRandomThunderTimeMax = 240.0f;
    [SerializeField] private float _fLightningIntensity = 5.0f;
    [SerializeField] private float _fLightningRange = 500.0f;
    [SerializeField] private float _fTimeBeforeLightningLightTurnsOff = 0.4f;
    [SerializeField] private AudioClip _adThunderSound;
    [SerializeField] private float _fLightningVolume = 1.0f;

    [SerializeField] private float _fLightningRangeFromWeatherMasterMin = 10.0f;
    [SerializeField] private float _fLightningRangeFromWeatherMasterMax = 50.0f; // Sửa mặc định tránh trùng min
    [SerializeField] private float _fLightningHeight = 50.0f;

    /********** ----- GETTERS AND SETTERS ----- **********/
    public GameObject GetSet_gPartRain { get { return _gPartRain; } set { _gPartRain = value; } }
    public float GetSet_RandomThunderTimeMin { get { return _fRandomThunderTimeMin; } set { _fRandomThunderTimeMin = value; } }
    public float GetSet_RandomThunderTimeMax { get { return _fRandomThunderTimeMax; } set { _fRandomThunderTimeMax = value; } }
    public float GetSet_fLightningIntensity { get { return _fLightningIntensity; } set { _fLightningIntensity = value; } }
    public float GetSet_fLightningRange { get { return _fLightningRange; } set { _fLightningRange = value; } }
    public float GetSet_fTimeBeforeLightningLightTurnsOff { get { return _fTimeBeforeLightningLightTurnsOff; } set { _fTimeBeforeLightningLightTurnsOff = value; } }
    public AudioClip GetSet_adThunderSound { get { return _adThunderSound; } set { _adThunderSound = value; } }
    public float GetSet_fLightningVolume { get { return _fLightningVolume; } set { _fLightningVolume = value; } }
    public float GetSet_fLightningRangeFromWeatherMasterMin { get { return _fLightningRangeFromWeatherMasterMin; } set { _fLightningRangeFromWeatherMasterMin = value; } }
    public float GetSet_fLightningRangeFromWeatherMasterMax { get { return _fLightningRangeFromWeatherMasterMax; } set { _fLightningRangeFromWeatherMasterMax = value; } }
    public float GetSet_fLightningHeight { get { return _fLightningHeight; } set { _fLightningHeight = value; } }
    public float Get_TimerForNextLighting { get { return _fTimerForNextLighting; } }
    public float Get_TimeBeforeNextLighting { get { return _fTimeForNextLighting; } }

    private void Start()
    {
        clWeatherController = (Weather_Controller)this.GetComponent(typeof(Weather_Controller));

        if (_gPartRain != null)
        {
            _cachedRainParticleSystem = _gPartRain.GetComponent<ParticleSystem>();
        }

        if (_bUseMorningFog == false)
            _fFogMorningAmount = _fFogAmount;

        _fSoundVolumeIn = _fSoundVolume;
        _fSoundVolumeOut = _fSoundVolume;
        _fEndParticleTimerStart = 0.0f;
        _fEndParticleTimerEnd = 5.0f;

        if (_bUsingSound && _gPartRain != null)
        {
            if (_adAmbientSound != null)
            {
                AudioSource rainAudio = _gPartRain.GetComponent<AudioSource>();
                if (rainAudio == null)
                {
                    rainAudio = _gPartRain.AddComponent<AudioSource>();
                    Debug.LogWarning("There was no AUDIOSOURCE on " + _gPartRain + " this is now added");
                }

                _bGotAudioSource = true;
                rainAudio.clip = _adAmbientSound;
                rainAudio.volume = 0.0f;
                rainAudio.loop = true;
            }
            else
            {
                Debug.Log("There is no AMBIENT SOUND attached to the WeatherController on type: " + clWeatherController.en_CurrWeather);
            }
        }

        _fTimeForNextLighting = Random.Range(_fRandomThunderTimeMin + 500.0f, _fRandomThunderTimeMax + 1000.0f);
        _fTimerForNextLighting = 0.0f;
    }

    public override void Init()
    {
        base.Init();
        TurnOnRain();

        if (_cachedRainParticleSystem != null)
        {
            var emission = _cachedRainParticleSystem.emission;
            emission.enabled = true; // Sửa lỗi cú pháp Unity cũ
        }

        // Khởi tạo Object Sét tối ưu, không dùng Instantiate lỗi lý thuyết
        if (_gLighting == null)
        {
            _gLighting = new GameObject("ThunderLighting");
            _lightingComponent = _gLighting.AddComponent<Light>();
            _lightingAudioSource = _gLighting.AddComponent<AudioSource>();

            _lightingComponent.range = _fLightningRange;
            _lightingComponent.intensity = 0.0f;
        }

        _fTimeForNextLighting = Random.Range(_fRandomThunderTimeMin, _fRandomThunderTimeMax);
        _fTimerForNextLighting = 0.0f;
    }

    private void Update()
    {
        UpdateWeather();
        UpdateThunder();

        if (_bUseInit)
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
            clWeatherController.UpdateAllWeather(_fSunrise_LightIntensity, _cSunrise_LightColor, 0.0f, _cNight_MoonLightColor, _cSunrise_SkyTintColor, _cSunrise_SkyGroundColor, _cCloudColor, _fFogMorningAmount, _cFogColor, _fFadeTime);
            clWeatherController.DeactivateTimesetParticle(_pNightParticle);
            clWeatherController.ActivateTimesetParticle(_pSunriseParticle);
        }
        else if (tod.enCurrTimeset == ToD_Base.Timeset.DAY)
        {
            clWeatherController.UpdateAllWeather(_fDay_LightIntensity, _cDay_LightColor, 0.0f, _cNight_MoonLightColor, _cDay_SkyTintColor, _cDay_SkyGroundColor, _cCloudColor, _fFogAmount, _cFogColor, _fFadeTime);
            clWeatherController.DeactivateTimesetParticle(_pSunriseParticle);
            clWeatherController.ActivateTimesetParticle(_pDayParticle);
        }
        else if (tod.enCurrTimeset == ToD_Base.Timeset.SUNSET)
        {
            clWeatherController.UpdateAllWeather(_fSunset_LightIntensity, _cSunset_LightColor, 0.0f, _cNight_MoonLightColor, _cSunset_SkyTintColor, _cSunset_SkyGroundColor, _cCloudColor, _fFogAmount, _cFogColor, _fFadeTime);
            clWeatherController.DeactivateTimesetParticle(_pDayParticle);
            clWeatherController.ActivateTimesetParticle(_pSunsetParticle);
        }
        else if (tod.enCurrTimeset == ToD_Base.Timeset.NIGHT)
        {
            clWeatherController.UpdateAllWeather(_fNight_LightIntensity, _cNight_LightColor, _fNight_MoonLightIntensity, _cNight_MoonLightColor, _cNight_SkyTintColor, _cNight_SkyGroundColor, _cCloudColor, _fFogAmount, _cFogColor, _fFadeTime);
            clWeatherController.DeactivateTimesetParticle(_pSunsetParticle);
            clWeatherController.ActivateTimesetParticle(_pNightParticle);
        }
    }

    private void DifferentFadeTimes()
    {
        var tod = clWeatherController.gTimeOfDay.GetComponent<ToD_Base>();
        if (tod.enCurrTimeset == ToD_Base.Timeset.SUNRISE)
        {
            clWeatherController.UpdateAllWeather(_fSunrise_LightIntensity, _cSunrise_LightColor, 0.0f, _cNight_MoonLightColor, _cSunrise_SkyTintColor, _cSunrise_SkyGroundColor, _cCloudColor, _fFogMorningAmount, _cFogColor, _fSunriseFadeTime);
            clWeatherController.DeactivateTimesetParticle(_pNightParticle);
            clWeatherController.ActivateTimesetParticle(_pSunriseParticle);
        }
        else if (tod.enCurrTimeset == ToD_Base.Timeset.DAY)
        {
            clWeatherController.UpdateAllWeather(_fDay_LightIntensity, _cDay_LightColor, 0.0f, _cNight_MoonLightColor, _cDay_SkyTintColor, _cDay_SkyGroundColor, _cCloudColor, _fFogAmount, _cFogColor, _fDayFadeTime);
            clWeatherController.DeactivateTimesetParticle(_pSunriseParticle);
            clWeatherController.ActivateTimesetParticle(_pDayParticle);
        }
        else if (tod.enCurrTimeset == ToD_Base.Timeset.SUNSET)
        {
            clWeatherController.UpdateAllWeather(_fSunset_LightIntensity, _cSunset_LightColor, 0.0f, _cNight_MoonLightColor, _cSunset_SkyTintColor, _cSunset_SkyGroundColor, _cCloudColor, _fFogAmount, _cFogColor, _fSunsetFadeTime);
            clWeatherController.DeactivateTimesetParticle(_pDayParticle);
            clWeatherController.ActivateTimesetParticle(_pSunsetParticle);
        }
        else if (tod.enCurrTimeset == ToD_Base.Timeset.NIGHT)
        {
            clWeatherController.UpdateAllWeather(_fNight_LightIntensity, _cNight_LightColor, _fNight_MoonLightIntensity, _cNight_MoonLightColor, _cNight_SkyTintColor, _cNight_SkyGroundColor, _cCloudColor, _fFogAmount, _cFogColor, _fNightFadeTime);
            clWeatherController.DeactivateTimesetParticle(_pSunsetParticle);
            clWeatherController.ActivateTimesetParticle(_pNightParticle);
        }
    }

    public void UpdateThunder()
    {
        _fTimerForNextLighting += Time.deltaTime;

        if (_fTimerForNextLighting >= _fTimeForNextLighting)
            Lightning();
    }

    private void Lightning()
    {
        if (_gLighting != null)
        {
            // SỬA LỖI: Chọn vị trí ngẫu nhiên chính xác trên cả X và Z
            float fRandXPosition = Random.Range(_fLightningRangeFromWeatherMasterMin, _fLightningRangeFromWeatherMasterMax);
            float fRandZPosition = Random.Range(_fLightningRangeFromWeatherMasterMin, _fLightningRangeFromWeatherMasterMax);

            _gLighting.transform.position = new Vector3(fRandXPosition, _fLightningHeight, fRandZPosition);

            if (_bUsingSound)
            {
                if (_adThunderSound != null && _lightingAudioSource != null)
                {
                    _lightingAudioSource.clip = _adThunderSound;
                    _lightingAudioSource.volume = _fLightningVolume; // Áp dụng biến âm lượng đã khai báo
                    _lightingAudioSource.Play();
                }
                else
                    Debug.LogWarning("You have no thunder sound attached to: " + this.gameObject);
            }

            StartCoroutine(ControlLightingLightOnOff());

            _fTimerForNextLighting = 0.0f;
            _fTimeForNextLighting = Random.Range(_fRandomThunderTimeMin, _fRandomThunderTimeMax);
        }
    }

    IEnumerator ControlLightingLightOnOff()
    {
        if (_lightingComponent != null)
        {
            _lightingComponent.intensity = _fLightningIntensity;
            yield return new WaitForSeconds(_fTimeBeforeLightningLightTurnsOff);
        }

        if (_lightingComponent != null)
        {
            _lightingComponent.intensity = 0.0f;
            yield return null;
        }
    }

    private void TurnOnRain()
    {
        if (_gPartRain != null)
        {
            if (_gPartRain.activeInHierarchy == false)
            {
                _gPartRain.SetActive(true);

                if (_bUsingSound)
                    TurnOnSound(_gPartRain);
            }
        }
        else
            Debug.LogError("We are missing rain particles on: " + this.gameObject + " For weather type: THUNDERSTORM");
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

        if (_gPartRain != null && _gPartRain.activeInHierarchy)
        {
            _fEndParticleTimerStart += Time.deltaTime;

            // SỬA LỖI: Chuyển sang cú pháp mới hệ thống Emission của Unity
            var emission = _cachedRainParticleSystem.emission;
            emission.enabled = false;

            _fTimerForNextLighting = 0.0f;
            _fTimeForNextLighting = 0.0f;

            if (_gLighting != null)
            {
                Destroy(_gLighting);
                _gLighting = null; // Trả về null để hàm Init sau tạo lại sạch sẽ
            }

            if (_bTurnOffSoundAtExit)
            {
                if (_bUsingSound)
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