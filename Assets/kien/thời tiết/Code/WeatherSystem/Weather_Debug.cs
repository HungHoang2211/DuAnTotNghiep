using UnityEngine;
using System.Collections;

public class Weather_Debug : MonoBehaviour
{
    private Weather_Controller _clWeatherController;
    private bool _bWeatherDebugOn = false;

    void Start()
    {
        _clWeatherController = GetComponent<Weather_Controller>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.O)) _bWeatherDebugOn = !_bWeatherDebugOn;

        if (_clWeatherController == null) return;

        // Phím tắt nhanh
        if (Input.GetKeyDown(KeyCode.Alpha1)) _clWeatherController.UseWeatherTypeDebug(1); // SUN
        if (Input.GetKeyDown(KeyCode.Alpha2)) _clWeatherController.UseWeatherTypeDebug(2); // RAIN
    }

    void OnGUI()
    {
        if (!_bWeatherDebugOn || _clWeatherController == null) return;

        GUI.Box(new Rect(10, 10, 250, 150), "Weather System Debug");

        if (GUI.Button(new Rect(20, 40, 210, 30), "Trời Nắng (Sun) [Phím 1]"))
            _clWeatherController.UseWeatherTypeDebug(1);

        if (GUI.Button(new Rect(20, 80, 210, 30), "Trời Mưa (Rain) [Phím 2]"))
            _clWeatherController.UseWeatherTypeDebug(2);

        GUI.Label(new Rect(20, 120, 210, 20), "Thời tiết hiện tại: " + _clWeatherController.en_CurrWeather.ToString());
    }
}