using UnityEngine;

public class AdvancedDayNight : MonoBehaviour
{
    [Header("Time Settings")]
    public float dayDuration = 120f;

    [Range(0, 1)]
    public float timeOfDay;

    [Header("Lights")]
    public Light sun;
    public Light moon;

    [Header("Sun Settings")]
    public Gradient sunColor;
    public AnimationCurve sunIntensity;

    [Header("Moon Settings")]
    public AnimationCurve moonIntensity;

    [Header("Fog")]
    public Gradient fogColor;

    void Update()
    {
        // Time chạy từ 0 -> 1
        timeOfDay += Time.deltaTime / dayDuration;

        if (timeOfDay >= 1f)
        {
            timeOfDay = 0f;
        }

        UpdateLighting();
    }

    void UpdateLighting()
    {
        float sunAngle = timeOfDay * 360f;

        // ===== SUN =====
        sun.transform.rotation =
            Quaternion.Euler(sunAngle - 90f, 170f, 0);

        sun.color = sunColor.Evaluate(timeOfDay);

        sun.intensity =
            sunIntensity.Evaluate(timeOfDay);

        // ===== MOON =====
        moon.transform.rotation =
            Quaternion.Euler(sunAngle + 90f, 170f, 0);

        moon.intensity =
            moonIntensity.Evaluate(timeOfDay);

        // ===== FOG =====
        RenderSettings.fogColor =
            fogColor.Evaluate(timeOfDay);

        // Update realtime lighting
        DynamicGI.UpdateEnvironment();
        RenderSettings.fogColor =
    fogColor.Evaluate(timeOfDay);
    }
}