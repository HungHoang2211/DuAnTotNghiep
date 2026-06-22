using UnityEngine;
using System.Collections;

public class Weather_Controller : MonoBehaviour
{
    private bool _bChangeWeather;
    private int _iNewWeather;
    private bool _bStartWeatherChange;
    private float _fTimeChangeWeatherStart;
    private float _fTimeChangeWeatherEnd = 5.0f; // Thời gian chuyển giao thời tiết (5 giây)

    [SerializeField] private bool _bUsingProceduralSkybox;
    [SerializeField] private bool _bUseSun = true;
    [SerializeField] private bool _bUseRain = true;
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

    private ToD_Base _cachedToD;
    private Weather_Sun _weatherSun;
    private Weather_Rain _weatherRain;

    public enum WeatherType
    {
        RANDOM = 0,
        SUN = 1,
        RAIN = 2
    }

    [SerializeField] private WeatherType _enCurrWeather = WeatherType.SUN;
    private WeatherType _enNewWeather;

    public WeatherType en_CurrWeather { get { return _enCurrWeather; } }
    public WeatherType en_NewWeather { get { return _enNewWeather; } }
    public float GetSet_fCurrTemp { get { return _fCurrTemp; } set { _fCurrTemp = value; } }
    public bool Get_bStartWeatherChange { get { return _bStartWeatherChange; } }
    public int Get_iAmountOfDaysToNewWeather { get { return _iAmountOfDaysToNewWeather; } }
    public int GetSet_iAmountOfDaysSinceLastWeather { get { return _iAmountOfDaysSinceLastWeather; } set { _iAmountOfDaysSinceLastWeather = value; } }

    void Awake()
    {
        if (gTimeOfDay != null) _cachedToD = gTimeOfDay.GetComponent<ToD_Base>();
        _weatherSun = GetComponent<Weather_Sun>();
        _weatherRain = GetComponent<Weather_Rain>();
    }

    void Start()
    {
        _bChangeWeather = false;
        _bStartWeatherChange = false;
        _iAmountOfDaysSinceLastWeather = 0;

        if (_bUseRandomDaysWeather)
            _iAmountOfDaysToNewWeather = Random.Range(_iMinAmountOfDaysToNewWeather, _iMaxAmountOfDaysToNewWeather);
        else
            _iAmountOfDaysToNewWeather = _iChangeWeatherAfterDays;

        SetInitialWeather();
    }

    void Update()
    {
        if (_bChangeWeather)
        {
            _bChangeWeather = false;
            ChooseNewWeather();
        }

        // ĐÃ SỬA: Luôn cập nhật thời tiết mỗi khung hình để hạt mưa (Rain Particle) được Active kịp thời lúc chuyển giao
        UpdateCurrentWeather();

        if (_bStartWeatherChange)
        {
            TransitionWeather();
        }
    }

    private void SetInitialWeather()
    {
        if (_enCurrWeather == WeatherType.SUN && _weatherSun != null) _weatherSun.TurnOnSound(this.gameObject);
        else if (_enCurrWeather == WeatherType.RAIN && _weatherRain != null) _weatherRain.TurnOnSound(this.gameObject);
    }

    private void ChooseNewWeather()
    {
        if (!_bUseRandomWeather) return;

        bool targetFound = false;
        int attempts = 0;
        while (!targetFound && attempts < 10)
        {
            attempts++;
            int checkWeather = Random.Range(1, 3); // Bốc ngẫu nhiên 1 (SUN) hoặc 2 (RAIN)
            if (checkWeather == (int)_enCurrWeather) continue;

            if (checkWeather == 1 && _bUseSun) { _enNewWeather = WeatherType.SUN; targetFound = true; }
            else if (checkWeather == 2 && _bUseRain) { _enNewWeather = WeatherType.RAIN; targetFound = true; }
        }

        if (targetFound)
        {
            StartWeatherTransition(_enNewWeather);
        }
    }

    private void StartWeatherTransition(WeatherType newWeather)
    {
        _enNewWeather = newWeather;
        _bStartWeatherChange = true;
        _fTimeChangeWeatherStart = 0.0f;

        if (_enNewWeather == WeatherType.SUN && _weatherSun != null) _weatherSun.TurnOnSound(this.gameObject);
        else if (_enNewWeather == WeatherType.RAIN && _weatherRain != null) _weatherRain.TurnOnSound(this.gameObject);
    }

    private void TransitionWeather()
    {
        _fTimeChangeWeatherStart += Time.deltaTime;
        float progress = _fTimeChangeWeatherStart / _fTimeChangeWeatherEnd;

        if (_enNewWeather == WeatherType.SUN && _weatherSun != null) _weatherSun.ForceWeatherChange();
        else if (_enNewWeather == WeatherType.RAIN && _weatherRain != null) _weatherRain.ForceWeatherChange();

        if (_enNewWeather != _enCurrWeather)
        {
            if (_enCurrWeather == WeatherType.SUN && _weatherSun != null) _weatherSun.ExitWeatherEffect(this.gameObject);
            else if (_enCurrWeather == WeatherType.RAIN && _weatherRain != null) _weatherRain.ExitWeatherEffect(this.gameObject);
        }

        if (progress >= 1.0f)
        {
            _bStartWeatherChange = false;
            _enCurrWeather = _enNewWeather;
        }
    }

    private void UpdateCurrentWeather()
    {
        // ĐÃ SỬA: Khi đang đổi thời tiết, cập nhật song song hiệu ứng ánh sáng/hạt của thời tiết MỚI đang chuyển tới
        if (_bStartWeatherChange)
        {
            if (_enNewWeather == WeatherType.SUN && _weatherSun != null) _weatherSun.RunWeather();
            else if (_enNewWeather == WeatherType.RAIN && _weatherRain != null) _weatherRain.RunWeather();
            return;
        }

        if (_enCurrWeather == WeatherType.SUN && _weatherSun != null) _weatherSun.RunWeather();
        else if (_enCurrWeather == WeatherType.RAIN && _weatherRain != null) _weatherRain.RunWeather();
    }

    public void UpdateAllWeather(float lightInt, Color lightCol, float moonInt, Color moonCol, Color skyTint, Color skyGround, Color cloudCol, float fogAmount, Color fogCol, float fadeTime)
    {
        if (_cachedToD == null) return;

        if (_cachedToD.lSun != null)
        {
            _cachedToD.lSun.intensity = Mathf.Lerp(_cachedToD.lSun.intensity, lightInt, Time.deltaTime / fadeTime);
            _cachedToD.lSun.color = Color.Lerp(_cachedToD.lSun.color, lightCol, Time.deltaTime / fadeTime);
        }

        if (_bUsingProceduralSkybox && matSkybox != null)
        {
            matSkybox.SetColor("_SkyTint", Color.Lerp(matSkybox.GetColor("_SkyTint"), skyTint, Time.deltaTime / fadeTime));
            matSkybox.SetColor("_GroundColor", Color.Lerp(matSkybox.GetColor("_GroundColor"), skyGround, Time.deltaTime / fadeTime));
        }

        if (matClouds != null)
        {
            matClouds.color = Color.Lerp(matClouds.color, cloudCol, Time.deltaTime / fadeTime);
        }

        RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, fogAmount, Time.deltaTime / fadeTime);
        RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, fogCol, Time.deltaTime / fadeTime);
    }

    public void ActivateTimesetParticle(GameObject particle)
    {
        if (particle == null) return;
        if (!particle.activeSelf) particle.SetActive(true);
        ParticleSystem ps = particle.GetComponent<ParticleSystem>();
        if (ps != null) { var emission = ps.emission; emission.enabled = true; }
    }

    public void DeactivateTimesetParticle(GameObject particle)
    {
        if (particle == null) return;
        ParticleSystem ps = particle.GetComponent<ParticleSystem>();
        if (ps != null) { var emission = ps.emission; emission.enabled = false; }
    }

    public void UseWeatherTypeDebug(int weatherTypeIndex)
    {
        if (weatherTypeIndex == 0)
        {
            _bChangeWeather = true;
        }
        else
        {
            _iNewWeather = weatherTypeIndex;
            StartWeatherTransition((WeatherType)weatherTypeIndex);
        }
    }

    // ĐÃ THÊM: Logic xử lý qua ngày mới chuẩn xác, tự động đếm tích lũy để kích hoạt đổi thời tiết tự động
    public void OnNewDayArrived()
    {
        _iAmountOfDaysSinceLastWeather += 1;

        if (_iAmountOfDaysSinceLastWeather >= _iAmountOfDaysToNewWeather)
        {
            _iAmountOfDaysSinceLastWeather = 0;

            if (_bUseRandomDaysWeather)
                _iAmountOfDaysToNewWeather = Random.Range(_iMinAmountOfDaysToNewWeather, _iMaxAmountOfDaysToNewWeather);
            else
                _iAmountOfDaysToNewWeather = _iChangeWeatherAfterDays;

            _bChangeWeather = true; // Bật cờ cho Update() tự chọn thời tiết ngẫu nhiên mới
        }
    }
}