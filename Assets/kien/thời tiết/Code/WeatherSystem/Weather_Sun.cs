using UnityEngine;

public class Weather_Sun : MonoBehaviour
{
    private ToD_Base _clToDBase;
    private float _fTargetIntensity = 1f;
    private float _fLerpSpeed = 2f;
    private bool _bIsFadingOut = false;

    void Start()
    {
        _clToDBase = FindFirstObjectByType<ToD_Base>();
    }

    void Update()
    {
        if (_clToDBase == null) return;

        if (_bIsFadingOut)
        {
            // Trả cường độ sáng và âm thanh về 0 khi tắt script
            if (_clToDBase.lSun != null)
                _clToDBase.lSun.intensity = Mathf.MoveTowards(_clToDBase.lSun.intensity, 0f, Time.deltaTime * _fLerpSpeed);
            return;
        }

        // Logic điều tiết ánh sáng theo buổi (Timeset)
        switch (_clToDBase.enCurrTimeset)
        {
            case ToD_Base.Timeset.SUNRISE: _fTargetIntensity = 0.4f; break;
            case ToD_Base.Timeset.DAY: _fTargetIntensity = 1.2f; break;
            case ToD_Base.Timeset.SUNSET: _fTargetIntensity = 0.5f; break;
            case ToD_Base.Timeset.NIGHT: _fTargetIntensity = 0.0f; break;
        }

        if (_clToDBase.lSun != null)
            _clToDBase.lSun.intensity = Mathf.Lerp(_clToDBase.lSun.intensity, _fTargetIntensity, Time.deltaTime * _fLerpSpeed);
    }

    public void StartWeatherTransition(Weather_Controller.WeatherType targetWeather)
    {
        _bIsFadingOut = (targetWeather != Weather_Controller.WeatherType.SUN);
    }
}