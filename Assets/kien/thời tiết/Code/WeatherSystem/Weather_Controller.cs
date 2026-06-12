using UnityEngine;
using System.Collections;

public class Weather_Controller : MonoBehaviour
{
    /********** ----- VARIABLES ----- **********/

    private bool _bChangeWeather;
    private int _iNewWeather;
    private bool _bStartWeatherChange;
    private float _fTimeChangeWeatherStart;
    private float _fTimeChangeWeatherEnd;

    [SerializeField] private bool _bUsingProceduralSkybox;
    [SerializeField] private bool _bUseSun = true;
    [SerializeField] private bool _bUseCloudy = true;
    [SerializeField] private bool _bUseRain = true;
    [SerializeField] private bool _bUseThunderstorm = true;
    [SerializeField] private bool _bUseSnow = true;
    [SerializeField] private bool _bUseRandomWeather = true;
    [SerializeField] private bool _bUseRandomDaysWeather;

    private int _iAmountOfDaysToNewWeather;
    private int _iAmountOfDaysSinceLastWeather;

    [SerializeField] private int _iChangeWeatherAfterDays = 4;
    [SerializeField] private int _iMinAmountOfDaysToNewWeather = 1;
    [SerializeField] private int _iMaxAmountOfDaysToNewWeather = 10;

    private float _fCurrTemp;

    public GameObject gTimeOfDay;
    public Material matClouds;
    public Material matSkybox;

    // TỐI ƯU HIỆU SUẤT: Cache toàn bộ các Component để tránh gọi GetComponent liên tục
    private ToD_Base _cachedToD;
    private Weather_Sun _weatherSun;
    private Weather_Cloudy _weatherCloudy;
    private Weather_Rain _weatherRain;
    private Weather_Thunderstorm _weatherThunderstorm;
    private Weather_Snow _weatherSnow;

    public enum WeatherType
    {
        RANDOM,
        SUN,
        CLOUDY,
        RAIN,
        THUNDERSTORM,
        SNOW,
        NUMBEROFWEATHERTYPES
    };

    public WeatherType en_CurrWeather;

    [HideInInspector]
    public WeatherType en_LastWeather;

    /********** ----- GETTERS AND SETTERS ----- **********/

    public bool Get_bChangeWeather { get { return _bChangeWeather; } }
    public bool Get_bStartWeatherChange { get { return _bStartWeatherChange; } }
    public float Get_fTimeChangeWeatherStart { get { return _fTimeChangeWeatherStart; } }
    public int Get_iAmountOfDaysToNewWeather { get { return _iAmountOfDaysToNewWeather; } }

    public bool GetSet_bUsingProceduralSkybox { get { return _bUsingProceduralSkybox; } set { _bUsingProceduralSkybox = value; } }
    public bool GetSet_bUseSun { get { return _bUseSun; } set { _bUseSun = value; } }
    public bool GetSet_bUseCloudy { get { return _bUseCloudy; } set { _bUseCloudy = value; } }
    public bool GetSet_bUseRain { get { return _bUseRain; } set { _bUseRain = value; } }
    public bool GetSet_bUseThunderstorm { get { return _bUseThunderstorm; } set { _bUseThunderstorm = value; } }
    public bool GetSet_bUseSnow { get { return _bUseSnow; } set { _bUseSnow = value; } }
    public bool GetSet_bUseRandomWeather { get { return _bUseRandomWeather; } set { _bUseRandomWeather = value; } }
    public bool GetSet_bUseRandomDaysWeather { get { return _bUseRandomDaysWeather; } set { _bUseRandomDaysWeather = value; } }
    public int GetSet_iChangeWeatherAfterDays { get { return _iChangeWeatherAfterDays; } set { _iChangeWeatherAfterDays = value; } }
    public int GetSet_iAmountOfDaysSinceLastWeather { get { return _iAmountOfDaysSinceLastWeather; } set { _iAmountOfDaysSinceLastWeather = value; } }
    public int GetSet_iMinAmountOfDaysToNewWeather { get { return _iMinAmountOfDaysToNewWeather; } set { _iMinAmountOfDaysToNewWeather = value; } }
    public int GetSet_iMaxAmountOfDaysToNewWeather { get { return _iMaxAmountOfDaysToNewWeather; } set { _iMaxAmountOfDaysToNewWeather = value; } }
    public float GetSet_fCurrTemp { get { return _fCurrTemp; } set { _fCurrTemp = value; } }

    void Start()
    {
        _fTimeChangeWeatherStart = 0.0f;
        _fTimeChangeWeatherEnd = 5.0f;

        // Khởi tạo và lưu cache linh hồn hệ thống thời gian/thời tiết từ ban đầu
        if (gTimeOfDay != null) _cachedToD = gTimeOfDay.GetComponent<ToD_Base>();

        _weatherSun = GetComponent<Weather_Sun>();
        _weatherCloudy = GetComponent<Weather_Cloudy>();
        _weatherRain = GetComponent<Weather_Rain>();
        _weatherThunderstorm = GetComponent<Weather_Thunderstorm>();
        _weatherSnow = GetComponent<Weather_Snow>();

        if (_bUseRandomDaysWeather == true)
            _iAmountOfDaysToNewWeather = Random.Range(_iMinAmountOfDaysToNewWeather, _iMaxAmountOfDaysToNewWeather);
        else
            _iAmountOfDaysToNewWeather = _iChangeWeatherAfterDays;

        if ((en_CurrWeather == WeatherType.RANDOM || en_CurrWeather == WeatherType.NUMBEROFWEATHERTYPES) && _bUseRandomWeather == true)
            PickRandomWeather();
        else if ((en_CurrWeather == WeatherType.RANDOM || en_CurrWeather == WeatherType.NUMBEROFWEATHERTYPES) && _bUseRandomWeather == false)
        {
            Debug.LogWarning("You haven't picked which weather to use, we default to SUN...");
            ExitCurrentWeather((int)WeatherType.SUN);
        }
        else
            EnterNewWeather((int)en_CurrWeather);
    }

    void Update()
    {
        if (_bUseRandomWeather == true)
        {
            if (_bUseRandomDaysWeather == true)
            {
                if (_iAmountOfDaysSinceLastWeather >= _iAmountOfDaysToNewWeather)
                {
                    _bChangeWeather = true;
                    _iAmountOfDaysSinceLastWeather = 0;
                    _iAmountOfDaysToNewWeather = Random.Range(_iMinAmountOfDaysToNewWeather, _iMaxAmountOfDaysToNewWeather);
                }
            }
            else
            {
                if (_iAmountOfDaysSinceLastWeather >= _iChangeWeatherAfterDays)
                {
                    _bChangeWeather = true;
                    _iAmountOfDaysSinceLastWeather = 0;
                }
            }
        }

        if (_bChangeWeather == true)
            PickRandomWeather();

        if (_bStartWeatherChange == true)
            ExitCurrentWeather(_iNewWeather);
    }

    private void PickRandomWeather()
    {
        int Weather = Random.Range(1, (int)WeatherType.NUMBEROFWEATHERTYPES);

        if (Weather != (int)en_CurrWeather)
            CheckIfWeatherTypeIsOn(Weather);
        else
            Debug.Log("We got the same weather no change will happen!");

        _bChangeWeather = false;
    }

    void CheckIfWeatherTypeIsOn(int NewWeatherType)
    {
        if (NewWeatherType == (int)WeatherType.SUN && _bUseSun) { _iNewWeather = NewWeatherType; _bStartWeatherChange = true; }
        else if (NewWeatherType == (int)WeatherType.CLOUDY && _bUseCloudy) { _iNewWeather = NewWeatherType; _bStartWeatherChange = true; }
        else if (NewWeatherType == (int)WeatherType.RAIN && _bUseRain) { _iNewWeather = NewWeatherType; _bStartWeatherChange = true; }
        else if (NewWeatherType == (int)WeatherType.THUNDERSTORM && _bUseThunderstorm) { _iNewWeather = NewWeatherType; _bStartWeatherChange = true; }
        else if (NewWeatherType == (int)WeatherType.SNOW && _bUseSnow) { _iNewWeather = NewWeatherType; _bStartWeatherChange = true; }
        else
        {
            Debug.Log("Weather type was not on, so we are trying again!");
            PickRandomWeather();
        }
    }

    void ChangeWeatherToSun()
    {
        en_CurrWeather = WeatherType.SUN;
        if (_weatherSun != null) { _weatherSun.enabled = true; _weatherSun.GetSet_bUseInit = true; }
    }

    void ChangeWeatherToCloudy()
    {
        en_CurrWeather = WeatherType.CLOUDY;
        if (_weatherCloudy != null) { _weatherCloudy.enabled = true; _weatherCloudy.GetSet_bUseInit = true; }
    }

    void ChangeWeatherToRain()
    {
        en_CurrWeather = WeatherType.RAIN;
        if (_weatherRain != null) { _weatherRain.enabled = true; _weatherRain.GetSet_bUseInit = true; }
    }

    void ChangeWeatherToThunderstorm()
    {
        en_CurrWeather = WeatherType.THUNDERSTORM;
        if (_weatherThunderstorm != null) { _weatherThunderstorm.enabled = true; _weatherThunderstorm.GetSet_bUseInit = true; }
    }

    void ChangeWeatherToSnow()
    {
        en_CurrWeather = WeatherType.SNOW;
        if (_weatherSnow != null) { _weatherSnow.enabled = true; _weatherSnow.GetSet_bUseInit = true; }
    }

    void ExitCurrentWeather(int NewWeatherType)
    {
        if (en_CurrWeather == WeatherType.RANDOM)
        {
            en_LastWeather = WeatherType.RANDOM;
            EnterNewWeather(NewWeatherType);
            _fTimeChangeWeatherStart = 0.0f;
            _bStartWeatherChange = false;
        }
        else if (en_CurrWeather == WeatherType.SUN)
        {
            en_LastWeather = WeatherType.SUN;
            _fTimeChangeWeatherStart += Time.deltaTime;

            if (_weatherSun != null) _weatherSun.ExitWeatherEffect(_weatherSun.GetSet_gSoundEffect);

            if (_fTimeChangeWeatherStart >= _fTimeChangeWeatherEnd)
            {
                if (_weatherSun != null) _weatherSun.enabled = false;
                EnterNewWeather(NewWeatherType);
                _fTimeChangeWeatherStart = 0.0f;
                _bStartWeatherChange = false;
            }
        }
        else if (en_CurrWeather == WeatherType.CLOUDY)
        {
            en_LastWeather = WeatherType.CLOUDY;
            _fTimeChangeWeatherStart += Time.deltaTime;

            if (_weatherCloudy != null) _weatherCloudy.ExitWeatherEffect(_weatherCloudy.GetSet_gSoundEffect);

            if (_fTimeChangeWeatherStart >= _fTimeChangeWeatherEnd)
            {
                if (_weatherCloudy != null) _weatherCloudy.enabled = false;
                EnterNewWeather(NewWeatherType);
                _fTimeChangeWeatherStart = 0.0f;
                _bStartWeatherChange = false;
            }
        }
        else if (en_CurrWeather == WeatherType.RAIN)
        {
            en_LastWeather = WeatherType.RAIN;
            _fTimeChangeWeatherStart += Time.deltaTime;

            if (_weatherRain != null) _weatherRain.ExitWeatherEffect(_weatherRain.GetSet_gPartRain);

            if (_fTimeChangeWeatherStart >= _fTimeChangeWeatherEnd)
            {
                if (_weatherRain != null) _weatherRain.enabled = false;
                EnterNewWeather(NewWeatherType);
                _fTimeChangeWeatherStart = 0.0f;
                _bStartWeatherChange = false;
            }
        }
        else if (en_CurrWeather == WeatherType.THUNDERSTORM)
        {
            en_LastWeather = WeatherType.THUNDERSTORM;
            _fTimeChangeWeatherStart += Time.deltaTime;

            if (_weatherThunderstorm != null) _weatherThunderstorm.ExitWeatherEffect(_weatherThunderstorm.GetSet_gPartRain);

            if (_fTimeChangeWeatherStart >= _fTimeChangeWeatherEnd)
            {
                if (_weatherThunderstorm != null) _weatherThunderstorm.enabled = false;
                EnterNewWeather(NewWeatherType);
                _fTimeChangeWeatherStart = 0.0f;
                _bStartWeatherChange = false;
            }
        }
        else if (en_CurrWeather == WeatherType.SNOW)
        {
            en_LastWeather = WeatherType.SNOW;
            _fTimeChangeWeatherStart += Time.deltaTime;

            if (_weatherSnow != null) _weatherSnow.ExitWeatherEffect(_weatherSnow.GetSet_gPartSnow);

            if (_fTimeChangeWeatherStart >= _fTimeChangeWeatherEnd)
            {
                if (_weatherSnow != null) _weatherSnow.enabled = false;
                EnterNewWeather(NewWeatherType);
                _fTimeChangeWeatherStart = 0.0f;
                _bStartWeatherChange = false;
            }
        }
    }

    private void EnterNewWeather(int NewWeather)
    {
        if (NewWeather == (int)WeatherType.SUN) ChangeWeatherToSun();
        else if (NewWeather == (int)WeatherType.CLOUDY) ChangeWeatherToCloudy();
        else if (NewWeather == (int)WeatherType.RAIN) ChangeWeatherToRain();
        else if (NewWeather == (int)WeatherType.THUNDERSTORM) ChangeWeatherToThunderstorm();
        else if (NewWeather == (int)WeatherType.SNOW) ChangeWeatherToSnow();
    }

    public void UpdateAllWeather(float sunIntensity, Color sunLightColor, float moonIntensity, Color moonLightColor, Color skyTint, Color skyGround, Color cloudColor, float fogDensity, Color fogColor, float fadeTime)
    {
        if (_cachedToD == null) return;

        // TỐI ƯU: Sử dụng biến ToD đã được cache sẵn
        if (_cachedToD.lSun != null)
        {
            _cachedToD.lSun.intensity = Mathf.Lerp(_cachedToD.lSun.intensity, sunIntensity, Time.deltaTime / fadeTime);
            _cachedToD.lSun.color = Color.Lerp(_cachedToD.lSun.color, sunLightColor, Time.deltaTime / fadeTime);
        }

        if (_cachedToD.GetSet_bUseMoon == true && _cachedToD.lMoon != null)
        {
            _cachedToD.lMoon.intensity = Mathf.Lerp(_cachedToD.lMoon.intensity, moonIntensity, Time.deltaTime / fadeTime);
            _cachedToD.lMoon.color = Color.Lerp(_cachedToD.lMoon.color, moonLightColor, Time.deltaTime / fadeTime);
        }

        if (RenderSettings.skybox != null)
        {
            if (_bUsingProceduralSkybox == false)
                RenderSettings.skybox.SetColor("_Tint", Color.Lerp(RenderSettings.skybox.GetColor("_Tint"), skyTint, Time.deltaTime / fadeTime));
            else
            {
                RenderSettings.skybox.SetColor("_SkyTint", Color.Lerp(RenderSettings.skybox.GetColor("_SkyTint"), skyTint, Time.deltaTime / fadeTime));
                RenderSettings.skybox.SetColor("_GroundColor", Color.Lerp(RenderSettings.skybox.GetColor("_GroundColor"), skyGround, Time.deltaTime / fadeTime));
            }
        }

        if (matClouds != null)
            matClouds.color = Color.Lerp(matClouds.color, cloudColor, Time.deltaTime / fadeTime);
        else
            Debug.LogWarning("We have no cloud material attached to:" + this.gameObject);

        RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, fogDensity, Time.deltaTime / fadeTime);
        RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, fogColor, Time.deltaTime / fadeTime);
    }

    // SỬA TRIỆT ĐỂ WARNING: Cập nhật hàm bật hiệu ứng bằng cấu trúc emission mới
    public void ActivateTimesetParticle(GameObject CurrParticles)
    {
        if (CurrParticles == null) return;

        ParticleSystem ps = CurrParticles.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var emission = ps.emission;
            if (!emission.enabled)
            {
                emission.enabled = true;

                for (int iii = 0; iii < CurrParticles.transform.childCount; ++iii)
                {
                    ParticleSystem childPs = CurrParticles.transform.GetChild(iii).GetComponent<ParticleSystem>();
                    if (childPs != null)
                    {
                        var childEmission = childPs.emission;
                        childEmission.enabled = true;
                    }
                }
            }
        }
        else
        {
            for (int iii = 0; iii < CurrParticles.transform.childCount; ++iii)
            {
                ParticleSystem childPs = CurrParticles.transform.GetChild(iii).GetComponent<ParticleSystem>();
                if (childPs != null)
                {
                    var childEmission = childPs.emission;
                    childEmission.enabled = true;
                }
            }
        }
    }

    // SỬA TRIỆT ĐỂ WARNING: Cập nhật hàm tắt hiệu ứng bằng cấu trúc emission mới
    public void DeactivateTimesetParticle(GameObject CurrParticles)
    {
        if (CurrParticles == null) return;

        ParticleSystem ps = CurrParticles.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var emission = ps.emission;
            if (emission.enabled)
            {
                emission.enabled = false;

                for (int iii = 0; iii < CurrParticles.transform.childCount; ++iii)
                {
                    ParticleSystem childPs = CurrParticles.transform.GetChild(iii).GetComponent<ParticleSystem>();
                    if (childPs != null)
                    {
                        var childEmission = childPs.emission;
                        childEmission.enabled = false;
                    }
                }
            }
        }
        else
        {
            for (int iii = 0; iii < CurrParticles.transform.childCount; ++iii)
            {
                ParticleSystem childPs = CurrParticles.transform.GetChild(iii).GetComponent<ParticleSystem>();
                if (childPs != null)
                {
                    var childEmission = childPs.emission;
                    childEmission.enabled = false;
                }
            }
        }
    }

    public void UseWeatherTypeDebug(int WeatherType)
    {
        if (WeatherType == 0)
            _bChangeWeather = true;
        else
        {
            _iNewWeather = WeatherType;
            _bStartWeatherChange = true;
        }
    }
}