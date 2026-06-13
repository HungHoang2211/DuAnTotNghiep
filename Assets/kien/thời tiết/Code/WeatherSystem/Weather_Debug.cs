using UnityEngine;
using System.Collections;

/// <summary>
/// Debug: Handles debug overlay and inputs for our Weather system
/// </summary>
public class Weather_Debug : MonoBehaviour
{
    private Weather_Controller _clWeatherController;
    private bool _bWeatherDebugOn;
    private bool _bMoreDebugInfo;

    public GUISkin guiDebugSkin;

    void Start()
    {
        // TỐI ƯU: Sử dụng Generic GetComponent để thay thế cách lấy Component kiểu cũ
        _clWeatherController = GetComponent<Weather_Controller>();
        _bWeatherDebugOn = false;
        _bMoreDebugInfo = false;
    }

    void Update()
    {
        BasicDebugControls();

        // Chỉ kiểm tra các phím tắt nâng cao khi bảng thông tin chi tiết đang mở
        if (_bWeatherDebugOn && _bMoreDebugInfo)
            AdvancedDebugControls();
    }

    private void BasicDebugControls()
    {
        if (_clWeatherController == null) return;

        // Bật / tắt bảng hiển thị Debug bằng phím O
        if (Input.GetKeyDown(KeyCode.O))
        {
            _bWeatherDebugOn = !_bWeatherDebugOn;
        }

        // Bật / tắt hướng dẫn đổi thời tiết bằng phím H
        if (Input.GetKeyDown(KeyCode.H))
        {
            _bMoreDebugInfo = !_bMoreDebugInfo;
        }
    }

    private void AdvancedDebugControls()
    {
        if (_clWeatherController == null) return;

        // ĐỒNG BỘ: Loại bỏ các thời tiết đã xóa, chỉ giữ lại các lựa chọn hợp lệ
        if (Input.GetKeyDown(KeyCode.Alpha1))
            _clWeatherController.UseWeatherTypeDebug(0); // Lấy thời tiết ngẫu nhiên (RANDOM)
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            _clWeatherController.UseWeatherTypeDebug((int)Weather_Controller.WeatherType.SUN);
        else if (Input.GetKeyDown(KeyCode.Alpha3))
            _clWeatherController.UseWeatherTypeDebug((int)Weather_Controller.WeatherType.RAIN);
    }

    void OnGUI()
    {
        if (_clWeatherController == null) return;

        if (guiDebugSkin != null)
        {
            GUI.skin = guiDebugSkin;
        }
        // ĐÃ SỬA: Loại bỏ Debug.Log ở đây để tránh làm tràn màn hình Console gây đứng game

        if (_bWeatherDebugOn == true)
        {
            // Tiêu đề hệ thống Debug
            GUI.color = Color.yellow;
            GUI.Label(new Rect(Screen.width / 2 - 120, 20, 240, 30), "Debugging: WEATHER SYSTEM");
            GUI.Label(new Rect(Screen.width / 2 - 225, 40, 450, 30), "Press H for more Debug information and controls");

            // Thông tin chi tiết về thời tiết hiện tại
            GUI.color = Color.red;

            GUI.Label(new Rect(20, 60, 300, 30), "Current weather:");
            GUI.Label(new Rect(320, 60, 100, 30), _clWeatherController.en_CurrWeather.ToString());

            GUI.Label(new Rect(20, 90, 300, 30), "Last weather:");
            GUI.Label(new Rect(320, 90, 100, 30), _clWeatherController.en_LastWeather.ToString());

            GUI.Label(new Rect(20, 120, 300, 30), "Current temperature:");
            GUI.Label(new Rect(320, 120, 100, 30), _clWeatherController.GetSet_fCurrTemp.ToString("F2") + "°C");

            // Tiến trình chu kỳ thay đổi thời tiết
            GUI.Label(new Rect(20, 180, 300, 30), "Next weather change (days):");
            GUI.Label(new Rect(320, 180, 100, 30), _clWeatherController.Get_iAmountOfDaysToNewWeather.ToString());

            GUI.Label(new Rect(20, 210, 300, 30), "Days since last weather change:");
            GUI.Label(new Rect(320, 210, 100, 30), _clWeatherController.GetSet_iAmountOfDaysSinceLastWeather.ToString());

            GUI.Label(new Rect(20, 240, 300, 30), "Weather changing now:");
            GUI.Label(new Rect(320, 240, 100, 30), _clWeatherController.Get_bStartWeatherChange.ToString());

            GUI.Label(new Rect(20, 270, 300, 30), "Transition Timer:");
            GUI.Label(new Rect(320, 270, 100, 30), _clWeatherController.Get_fTimeChangeWeatherStart.ToString("F1") + "s");

            // Bảng phím tắt nâng cao để test nhanh thời tiết (Hiện khi nhấn H)
            if (_bMoreDebugInfo == true)
            {
                GUI.color = Color.cyan;

                GUI.Label(new Rect(20, 320, 600, 30), "Press 1 to get a new RANDOM weather");
                GUI.Label(new Rect(20, 350, 600, 30), "Press 2 to force SUNNY weather");
                GUI.Label(new Rect(20, 380, 600, 30), "Press 3 to force RAINY weather");
            }
        }
    }
}