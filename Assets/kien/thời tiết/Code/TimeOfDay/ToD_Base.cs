using UnityEngine;
using System.Collections;

/// <summary>
/// Base: Time of Day
/// </summary>
public class ToD_Base : MonoBehaviour
{
    /********** ----- VARIABLES ----- **********/

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

    // TỐI ƯU HIỆU SUẤT: Cache script Weather_Controller để tránh gọi GetComponent trong Update liên tục
    private Weather_Controller _cachedWeatherController;

    public enum Timeset
    {
        SUNRISE,
        DAY,
        SUNSET,
        NIGHT
    };

    [HideInInspector]
    public Timeset enCurrTimeset;

    /********** ----- GETTERS AND SETTERS ----- **********/

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

        // Lưu cache sẵn bộ điều khiển thời tiết để tối ưu bộ nhớ
        if (gWeatherMaster != null)
        {
            _cachedWeatherController = gWeatherMaster.GetComponent<Weather_Controller>();
        }
    }

    void Update()
    {
        UpdateSunAndMoon();
        UpdateTimeset();

        // Xử lý vận tốc trôi của thời gian toàn cục
        _fCurrentTimeOfDay += (Time.deltaTime / _fSecondInAFullDay) * _fTimeMultiplier;

        // Tính toán định dạng thời gian dạng Số (Digital Hours/Minutes)
        _fCurrentHour = 24 * _fCurrentTimeOfDay;
        _fCurrentMinute = 60 * (_fCurrentHour - Mathf.Floor(_fCurrentHour));

        // Khi kết thúc chu kỳ một ngày (Vượt ngưỡng 1.0f)
        if (_fCurrentTimeOfDay >= 1.0f)
        {
            _fCurrentTimeOfDay = 0.0f;
            _iAmountOfDaysPlayed += 1;

            // TỐI ƯU: Sử dụng biến cache đã lưu từ Start thay vì dùng GetComponent liên tục
            if (_bUseWeather && _cachedWeatherController != null)
            {
                _cachedWeatherController.GetSet_iAmountOfDaysSinceLastWeather += 1;
            }
        }
    }

    void UpdateSunAndMoon()
    {
        // Điều hướng góc quay của ánh sáng mặt trời theo dòng chảy thời gian thực
        if (lSun != null)
        {
            lSun.transform.localRotation = Quaternion.Euler((_fCurrentTimeOfDay * 360) - 90, 170, 0);
        }

        // Điều hướng góc quay của mặt trăng nếu tính năng được kích hoạt
        if (_bUseMoon && lMoon != null)
        {
            lMoon.transform.localRotation = Quaternion.Euler((_fCurrentTimeOfDay * 360) - 270, 170, 0);
        }
    }

    void UpdateTimeset()
    {
        // Điều kiện rẽ nhánh chính xác tuyệt đối để thiết lập 4 khoảng mốc thời gian trong ngày
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
        else // Khoảng thời gian từ Đêm muộn cho đến trước Bình minh ngày hôm sau
        {
            if (enCurrTimeset != Timeset.NIGHT) SetCurrentTimeset(Timeset.NIGHT);
        }
    }

    void SetCurrentTimeset(Timeset currentTime)
    {
        enCurrTimeset = currentTime;
    }
}