using UnityEngine;
using System.Collections;

public class Weather_Controller : MonoBehaviour
{
    private bool _bChangeWeather;
    private bool _bStartWeatherChange;
    private float _fTimeChangeWeatherStart;
    private float _fTimeChangeWeatherEnd = 5.0f;

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

    private bool _bEnvBlendInitialized;
    private float _fEnvBlendElapsed;
    private float _fLightIntensityFrom, _fLightIntensityTarget;
    private Color _cLightColorFrom, _cLightColorTarget;
    private Color _cSkyTintFrom, _cSkyTintTarget;
    private Color _cSkyGroundFrom, _cSkyGroundTarget;
    private Color _cCloudFrom, _cCloudTarget;
    private float _fFogAmountFrom, _fFogAmountTarget;
    private Color _cFogColorFrom, _cFogColorTarget;

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
            int checkWeather = Random.Range(1, 3);
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

        Weather_Base targetWeather = (_enNewWeather == WeatherType.SUN) ? (Weather_Base)_weatherSun : (Weather_Base)_weatherRain;
        if (targetWeather != null)
        {
            _fTimeChangeWeatherEnd = Mathf.Max(targetWeather.GetCurrentFadeTime(), 0.01f);
        }

        if (_enNewWeather == WeatherType.SUN && _weatherSun != null) _weatherSun.TurnOnSound(this.gameObject);
        else if (_enNewWeather == WeatherType.RAIN && _weatherRain != null) _weatherRain.TurnOnSound(this.gameObject);
    }

    private void TransitionWeather()
    {
        _fTimeChangeWeatherStart += Time.deltaTime;
        float progress = _fTimeChangeWeatherStart / _fTimeChangeWeatherEnd;

        if (_enNewWeather != _enCurrWeather)
        {
            if (_enCurrWeather == WeatherType.SUN && _weatherSun != null) _weatherSun.ExitWeatherEffect(this.gameObject, progress);
            else if (_enCurrWeather == WeatherType.RAIN && _weatherRain != null) _weatherRain.ExitWeatherEffect(this.gameObject, progress);
        }

        if (progress >= 1.0f)
        {
            _bStartWeatherChange = false;
            _enCurrWeather = _enNewWeather;
        }
    }

    private void UpdateCurrentWeather()
    {
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

        bool targetChanged = !_bEnvBlendInitialized
            || !Mathf.Approximately(_fLightIntensityTarget, lightInt)
            || _cLightColorTarget != lightCol
            || _cSkyTintTarget != skyTint
            || _cSkyGroundTarget != skyGround
            || _cCloudTarget != cloudCol
            || !Mathf.Approximately(_fFogAmountTarget, fogAmount)
            || _cFogColorTarget != fogCol;

        if (targetChanged)
        {
            _fLightIntensityFrom = _cachedToD.lSun != null ? _cachedToD.lSun.intensity : lightInt;
            _cLightColorFrom = _cachedToD.lSun != null ? _cachedToD.lSun.color : lightCol;
            _cSkyTintFrom = (_bUsingProceduralSkybox && matSkybox != null) ? matSkybox.GetColor("_SkyTint") : skyTint;
            _cSkyGroundFrom = (_bUsingProceduralSkybox && matSkybox != null) ? matSkybox.GetColor("_GroundColor") : skyGround;
            _cCloudFrom = matClouds != null ? matClouds.color : cloudCol;
            _fFogAmountFrom = RenderSettings.fogDensity;
            _cFogColorFrom = RenderSettings.fogColor;

            _fLightIntensityTarget = lightInt;
            _cLightColorTarget = lightCol;
            _cSkyTintTarget = skyTint;
            _cSkyGroundTarget = skyGround;
            _cCloudTarget = cloudCol;
            _fFogAmountTarget = fogAmount;
            _cFogColorTarget = fogCol;

            _fEnvBlendElapsed = 0.0f;
            _bEnvBlendInitialized = true;
        }

        _fEnvBlendElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_fEnvBlendElapsed / Mathf.Max(fadeTime, 0.001f));

        if (_cachedToD.lSun != null)
        {
            _cachedToD.lSun.intensity = Mathf.Lerp(_fLightIntensityFrom, _fLightIntensityTarget, t);
            _cachedToD.lSun.color = Color.Lerp(_cLightColorFrom, _cLightColorTarget, t);
        }

        if (_bUsingProceduralSkybox && matSkybox != null)
        {
            matSkybox.SetColor("_SkyTint", Color.Lerp(_cSkyTintFrom, _cSkyTintTarget, t));
            matSkybox.SetColor("_GroundColor", Color.Lerp(_cSkyGroundFrom, _cSkyGroundTarget, t));
        }

        if (matClouds != null)
        {
            matClouds.color = Color.Lerp(_cCloudFrom, _cCloudTarget, t);
        }

        RenderSettings.fogDensity = Mathf.Lerp(_fFogAmountFrom, _fFogAmountTarget, t);
        RenderSettings.fogColor = Color.Lerp(_cFogColorFrom, _cFogColorTarget, t);
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
            StartWeatherTransition((WeatherType)weatherTypeIndex);
        }
    }

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

            _bChangeWeather = true;
        }
    }
}