using UnityEngine;

[System.Serializable]
public class ToD_EnvironmentSettings
{
    public float LightIntensity = 1.0f;
    public Color LightColor = Color.white;
    public float MoonIntensity = 0.0f;
    public Color MoonColor = Color.white;
    public Color SkyTintColor = Color.white;
    public Color SkyGroundColor = Color.white;
    public float FogAmount = 0.005f;
    public Color FogColor = Color.grey;
}