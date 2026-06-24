using UnityEngine;
using UnityEditor;
using System.Collections;

[ExecuteInEditMode]
[CustomEditor(typeof(Weather_Controller))]
public class WeatherController_Editor : Editor
{
    public bool bShowTips;

    private int iMinWidth = 30;
    private int iMedWidth = 60;
    private int iMaxWidth = 120;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawNewGUI();
        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }
    }

    private void DrawNewGUI()
    {
        Weather_Controller cl = target as Weather_Controller;

        // SHOW MORE TIPS
        GUILayout.BeginHorizontal();
        GUILayout.Label("Show more information");
        bShowTips = EditorGUILayout.Toggle(bShowTips, GUILayout.MaxWidth(iMinWidth));
        GUILayout.EndHorizontal();

        // GAMEOBJECTS AND MATERIALS
        EditorGUILayout.HelpBox(("Add gameobjects, lights and materials"), MessageType.None, true);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Time of day (Gameobject):");
        cl.gTimeOfDay = EditorGUILayout.ObjectField("", cl.gTimeOfDay, typeof(GameObject), true, GUILayout.MaxWidth(iMaxWidth)) as GameObject;
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Skybox (Material):");
        cl.matSkybox = EditorGUILayout.ObjectField("", cl.matSkybox, typeof(Material), true, GUILayout.MaxWidth(iMaxWidth)) as Material;
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Cloud (Material):");
        cl.matClouds = EditorGUILayout.ObjectField("", cl.matClouds, typeof(Material), true, GUILayout.MaxWidth(iMaxWidth)) as Material;
        GUILayout.EndHorizontal();

        // WEATHER TOGGLES (ĐÃ SỬA: Vẽ ô tích chọn chuẩn, không gọi hàm ép thời tiết nữa)
        EditorGUILayout.HelpBox(("Choose what weather type you want to use"), MessageType.None, true);

        SerializedProperty useSunProp = serializedObject.FindProperty("_bUseSun");
        SerializedProperty useRainProp = serializedObject.FindProperty("_bUseRain");
        SerializedProperty useRandomWeatherProp = serializedObject.FindProperty("_bUseRandomWeather");
        SerializedProperty useRandomDaysProp = serializedObject.FindProperty("_bUseRandomDaysWeather");

        if (useSunProp != null)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(useSunProp, new GUIContent("Use Sun:"));
            GUILayout.EndHorizontal();
        }
        if (useRainProp != null)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(useRainProp, new GUIContent("Use Rain:"));
            GUILayout.EndHorizontal();
        }

        // RANDOM WEATHER SETTINGS
        EditorGUILayout.HelpBox(("Random weather settings"), MessageType.None, true);
        if (useRandomWeatherProp != null)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(useRandomWeatherProp, new GUIContent("Use Random Weather:"));
            GUILayout.EndHorizontal();
        }
        if (useRandomDaysProp != null)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(useRandomDaysProp, new GUIContent("Use Random Days:"));
            GUILayout.EndHorizontal();
        }
    }
}