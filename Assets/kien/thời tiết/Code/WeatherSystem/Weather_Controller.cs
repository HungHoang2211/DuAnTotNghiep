using UnityEngine;
using System.Collections;

public class Weather_Controller : MonoBehaviour
{
    public enum WeatherType { SUN, RAIN }

    [Header("References")]
    public ToD_Base clToDBase;
    public MonoBehaviour scriptWeatherSun;
    public MonoBehaviour scriptWeatherRain;

    [Header("Settings")]
    public WeatherType en_CurrWeather = WeatherType.SUN;
    [SerializeField] private int _iMinDaysBetweenChange = 2;
    [SerializeField] private int _iMaxDaysBetweenChange = 5;
    [SerializeField] private float _fTransitionDuration = 5f;

    private int _iDaysUntilNextChange;
    private int _iDaysCountSinceLastChange;
    private bool _bIsTransitioning = false;

    void Start()
    {
        if (clToDBase == null) clToDBase = FindFirstObjectByType<ToD_Base>();

        if (clToDBase != null)
            clToDBase.OnNewDayStarted += HandleNewDay;

        _iDaysUntilNextChange = Random.Range(_iMinDaysBetweenChange, _iMaxDaysBetweenChange + 1);
        InitWeatherState();
    }

    void OnDestroy()
    {
        if (clToDBase != null) clToDBase.OnNewDayStarted -= HandleNewDay;
    }

    private void InitWeatherState()
    {
        scriptWeatherSun.enabled = (en_CurrWeather == WeatherType.SUN);
        scriptWeatherRain.enabled = (en_CurrWeather == WeatherType.RAIN);
    }

    private void HandleNewDay()
    {
        if (_bIsTransitioning) return;

        _iDaysCountSinceLastChange++;
        if (_iDaysCountSinceLastChange >= _iDaysUntilNextChange)
        {
            WeatherType nextWeather = (en_CurrWeather == WeatherType.SUN) ? WeatherType.RAIN : WeatherType.SUN;
            StartCoroutine(TransitionWeatherRoutine(nextWeather));
        }
    }

    // Hàm gọi từ Debug để ép đổi thời tiết lập tức
    public void UseWeatherTypeDebug(int typeIndex)
    {
        if (_bIsTransitioning) return;
        StartCoroutine(TransitionWeatherRoutine((WeatherType)typeIndex));
    }

    private IEnumerator TransitionWeatherRoutine(WeatherType targetWeather)
    {
        _bIsTransitioning = true;
        _iDaysCountSinceLastChange = 0;
        _iDaysUntilNextChange = Random.Range(_iMinDaysBetweenChange, _iMaxDaysBetweenChange + 1);

        // Kích hoạt đồng thời cả 2 script để chúng tự xử lý Fade-in / Fade-out lẫn nhau
        scriptWeatherSun.enabled = true;
        scriptWeatherRain.enabled = true;

        // Báo hiệu bắt đầu chuyển giao thông qua hàm có sẵn của script con
        scriptWeatherSun.SendMessage("StartWeatherTransition", targetWeather, SendMessageOptions.DontRequireReceiver);
        scriptWeatherRain.SendMessage("StartWeatherTransition", targetWeather, SendMessageOptions.DontRequireReceiver);

        yield return new WaitForSeconds(_fTransitionDuration);

        en_CurrWeather = targetWeather;
        scriptWeatherSun.enabled = (en_CurrWeather == WeatherType.SUN);
        scriptWeatherRain.enabled = (en_CurrWeather == WeatherType.RAIN);
        _bIsTransitioning = false;
    }
}