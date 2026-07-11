using UnityEngine;
using System.Collections;

public class ToD_Base : MonoBehaviour
{
    [SerializeField] private bool _bUseMoon = true;
    [SerializeField] private bool _bUseWeather = true;
    [SerializeField] private float _fSecondInAFullDay = 60.0f;
    [SerializeField] private float _fTimeMultiplier = 1.0f;

    [SerializeField, Range(0, 1)]
    private float _fCurrentTimeOfDay;

    private const float ONEHOURLENGTH = 1.0f / 24.0f;

    [SerializeField] private int _iStartHour;
    [SerializeField] private int _iSunriseStart;
    [SerializeField] private int _iDayStart;
    [SerializeField] private int _iSunsetStart;
    [SerializeField] private int _iNightStart;

    private float _fStartingHour;
    private float _fStartingSunrise;
    private float _fStartingDay;
    private float _fStartingSunset;
    private float _fStartingNight;

    private float _fCurrentHour;
    private float _fCurrentMinute;
    private int _iAmountOfDaysPlayed;

    public GameObject gWeatherMaster;
    public Light lSun;
    public Light lMoon;

    private Weather_Controller _cachedWeatherController;

    [Header("Environment Settings (Baseline dung chung cho moi loai thoi tiet)")]
    [SerializeField] private ToD_EnvironmentSettings _envSunrise = new ToD_EnvironmentSettings { LightIntensity = 0.5f };
    [SerializeField] private ToD_EnvironmentSettings _envDay = new ToD_EnvironmentSettings { LightIntensity = 1.0f };
    [SerializeField] private ToD_EnvironmentSettings _envSunset = new ToD_EnvironmentSettings { LightIntensity = 0.5f };
    [SerializeField] private ToD_EnvironmentSettings _envNight = new ToD_EnvironmentSettings { LightIntensity = 0.05f, LightColor = Color.blue, SkyTintColor = Color.black, SkyGroundColor = Color.black };

    public enum Timeset
    {
        SUNRISE,
        DAY,
        SUNSET,
        NIGHT
    };

    [HideInInspector]
    public Timeset enCurrTimeset;

    public float Get_fCurrentTimeOfDay { get { return _fCurrentTimeOfDay; } }
    public float Get_fCurrentHour { get { return _fCurrentHour; } }
    public float Get_fCurrentMinute { get { return _fCurrentMinute; } }
    public int Get_iAmountOfDaysPlayed { get { return _iAmountOfDaysPlayed; } }

    public bool GetSet_bUseMoon { get { return _bUseMoon; } set { _bUseMoon = value; } }
    public bool GetSet_bUseWeather { get { return _bUseWeather; } set { _bUseWeather = value; } }
    public float GetSet_fSecondInAFullDay { get { return _fSecondInAFullDay; } set { _fSecondInAFullDay = value; } }
    public float GetSet_fTimeMultiplier { get { return _fTimeMultiplier; } set { _fTimeMultiplier = value; } }
    public int GetSet_iStartHour { get { return _iStartHour; } set { _iStartHour = value; } }
    public int GetSet_iSunriseStart { get { return _iSunriseStart; } set { _iSunriseStart = value; } }
    public int GetSet_iDayStart { get { return _iDayStart; } set { _iDayStart = value; } }
    public int GetSet_iSunsetStart { get { return _iSunsetStart; } set { _iSunsetStart = value; } }
    public int GetSet_iNightStart { get { return _iNightStart; } set { _iNightStart = value; } }

    public ToD_EnvironmentSettings GetEnvironmentSettings(Timeset timeset)
    {
        switch (timeset)
        {
            case Timeset.SUNRISE: return _envSunrise;
            case Timeset.DAY: return _envDay;
            case Timeset.SUNSET: return _envSunset;
            case Timeset.NIGHT: return _envNight;
        }
        return _envDay;
    }

    void Start()
    {
        _fStartingHour = ONEHOURLENGTH * (float)_iStartHour;
        _fCurrentTimeOfDay = _fStartingHour;

        _fStartingSunrise = ONEHOURLENGTH * (float)_iSunriseStart;
        _fStartingDay = ONEHOURLENGTH * (float)_iDayStart;
        _fStartingSunset = ONEHOURLENGTH * (float)_iSunsetStart;
        _fStartingNight = ONEHOURLENGTH * (float)_iNightStart;

        _iAmountOfDaysPlayed = 0;
        _fCurrentHour = 0.0f;
        _fCurrentMinute = 0.0f;

        if (gWeatherMaster != null)
        {
            _cachedWeatherController = gWeatherMaster.GetComponent<Weather_Controller>();
        }
    }

    void Update()
    {
        UpdateSunAndMoon();
        UpdateTimeset();

        _fCurrentTimeOfDay += (Time.deltaTime / _fSecondInAFullDay) * _fTimeMultiplier;

        _fCurrentHour = 24 * _fCurrentTimeOfDay;
        _fCurrentMinute = 60 * (_fCurrentHour - Mathf.Floor(_fCurrentHour));

        if (_fCurrentTimeOfDay >= 1.0f)
        {
            _fCurrentTimeOfDay = 0.0f;
            _iAmountOfDaysPlayed += 1;

            if (_bUseWeather && _cachedWeatherController != null)
            {
                _cachedWeatherController.OnNewDayArrived();
            }
        }
    }

    void UpdateSunAndMoon()
    {
        float sunX = (_fCurrentTimeOfDay * 360f) - 90f;
        float moonX = (_fCurrentTimeOfDay * 360f) - 270f;

        if (lSun != null)
        {
            lSun.transform.rotation = Quaternion.AngleAxis(170f, Vector3.up) * Quaternion.AngleAxis(sunX, Vector3.right);
        }

        if (_bUseMoon && lMoon != null)
        {
            lMoon.transform.rotation = Quaternion.AngleAxis(170f, Vector3.up) * Quaternion.AngleAxis(moonX, Vector3.right);
        }
    }

    void UpdateTimeset()
    {
        if (_fCurrentTimeOfDay >= _fStartingSunrise && _fCurrentTimeOfDay <= _fStartingDay)
        {
            if (enCurrTimeset != Timeset.SUNRISE) SetCurrentTimeset(Timeset.SUNRISE);
        }
        else if (_fCurrentTimeOfDay >= _fStartingDay && _fCurrentTimeOfDay <= _fStartingSunset)
        {
            if (enCurrTimeset != Timeset.DAY) SetCurrentTimeset(Timeset.DAY);
        }
        else if (_fCurrentTimeOfDay >= _fStartingSunset && _fCurrentTimeOfDay <= _fStartingNight)
        {
            if (enCurrTimeset != Timeset.SUNSET) SetCurrentTimeset(Timeset.SUNSET);
        }
        else
        {
            if (enCurrTimeset != Timeset.NIGHT) SetCurrentTimeset(Timeset.NIGHT);
        }
    }

    void SetCurrentTimeset(Timeset currentTime)
    {
        enCurrTimeset = currentTime;
    }
}