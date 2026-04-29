using TMPro;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI timeText;

    [SerializeField] Light sun;
    [SerializeField] Light moon;

    [SerializeField] TimeSettings timeSettings;

    [Header("Ambient Colors")]
    [SerializeField] Color dayAmbient = new Color(0.5f, 0.5f, 0.5f);
    [SerializeField] Color nightAmbient = new Color(0.05f, 0.07f, 0.12f);

    [Header("Light Intensity")]
    [SerializeField] float sunMaxIntensity = 1.0f;
    [SerializeField] float moonMaxIntensity = 0.3f;

    TimeService service;

    void Start()
    {
        service = new TimeService(timeSettings);
    }

    void Update()
    {
        UpdateTimeOfDay();
        RotateCelestialBodies();
        UpdateLighting();
        HandleSpeedInput();
    }

    void HandleSpeedInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            timeSettings.timeMultiplier *= 2;
        if (Input.GetKeyDown(KeyCode.LeftShift))
            timeSettings.timeMultiplier /= 2;
    }

    void RotateCelestialBodies()
    {
        float sunAngle = service.CalculateSunAngle();
        sun.transform.rotation = Quaternion.AngleAxis(sunAngle, Vector3.right);
        moon.transform.rotation = Quaternion.AngleAxis(sunAngle + 180f, Vector3.right);
    }

    void UpdateLighting()
    {
        float sunAngleRad = service.CalculateSunAngle() * Mathf.Deg2Rad;

        // Ambient lerp mượt giữa đêm <-> ngày, đáy ở nửa đêm, đỉnh ở giữa trưa
        float ambientFactor = (Mathf.Sin(sunAngleRad) + 1f) * 0.5f;
        RenderSettings.ambientLight = Color.Lerp(nightAmbient, dayAmbient, ambientFactor);

        // Mặt trời sáng nhất giữa trưa, tắt khi xuống dưới chân trời
        sun.intensity = sunMaxIntensity * Mathf.Clamp01(Mathf.Sin(sunAngleRad));

        // Mặt trăng sáng nhất nửa đêm, tắt khi xuống dưới chân trời (lệch pha 180°)
        moon.intensity = moonMaxIntensity * Mathf.Clamp01(Mathf.Sin(sunAngleRad + Mathf.PI));
    }

    void UpdateTimeOfDay()
    {
        service.UpdateTime(Time.deltaTime);
        if (timeText != null)
        {
            timeText.text = service.CurrentTime.ToString("HH:mm");
        }
    }
}