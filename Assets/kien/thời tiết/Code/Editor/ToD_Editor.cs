using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(ToD_Base))]
public class ToD_Editor : Editor
{
    public bool bShowTips;

    private int iMinWidth = 30;
    private int iMedWidth = 80;
    private int iMaxWidth = 140;

    public override void OnInspectorGUI()
    {
        DrawToDGUI();
        EditorUtility.SetDirty(target);
    }

    private void DrawToDGUI()
    {
        ToD_Base cl = target as ToD_Base;

        GUILayout.BeginHorizontal();
        GUILayout.Label("Show more information");
        bShowTips = EditorGUILayout.Toggle(bShowTips, GUILayout.MaxWidth(iMinWidth));
        GUILayout.EndHorizontal();

        EditorGUILayout.HelpBox(("Add gameobjects, lights and materials"), MessageType.None, true);
        if (bShowTips == true)
            EditorGUILayout.HelpBox("Sun: Needs to be a directional light to cover the whole world. \n\nWeather master: This needs to be a prefab with all the weather scripts on it. (See or use example prefab in package)", MessageType.Info, true);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Sun (Directional light): ");
        cl.lSun = EditorGUILayout.ObjectField("", cl.lSun, typeof(Light), true, GUILayout.MaxWidth(iMedWidth)) as Light;
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Use Moon light: ");
        cl.GetSet_bUseMoon = EditorGUILayout.Toggle(cl.GetSet_bUseMoon, GUILayout.MaxWidth(iMinWidth));
        GUILayout.EndHorizontal();

        if (cl.GetSet_bUseMoon == true)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Moon (Directional light): ");
            cl.lMoon = EditorGUILayout.ObjectField("", cl.lMoon, typeof(Light), true, GUILayout.MaxWidth(iMedWidth)) as Light;
            GUILayout.EndHorizontal();
        }

        GUILayout.BeginHorizontal();
        GUILayout.Label("Use weather system: ");
        cl.GetSet_bUseWeather = EditorGUILayout.Toggle(cl.GetSet_bUseWeather, GUILayout.MaxWidth(iMinWidth));
        GUILayout.EndHorizontal();

        if (cl.GetSet_bUseWeather == true)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Weather master: ");
            cl.gWeatherMaster = EditorGUILayout.ObjectField("", cl.gWeatherMaster, typeof(GameObject), true, GUILayout.MaxWidth(iMedWidth)) as GameObject;
            GUILayout.EndHorizontal();
        }

        EditorGUILayout.HelpBox(("Time of day settings"), MessageType.None, true);
        if (bShowTips == true)
            EditorGUILayout.HelpBox("Length of a full (game) day and night cycle in seconds", MessageType.Info, true);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Full day cycle in seconds: ");
        cl.GetSet_fSecondInAFullDay = EditorGUILayout.FloatField("", cl.GetSet_fSecondInAFullDay, GUILayout.MaxWidth(iMedWidth));
        GUILayout.EndHorizontal();

        if (bShowTips == true)
            EditorGUILayout.HelpBox("*Uses 24 hour clock \n\nGame starting hour - Choose when you want the game to begin. \n\nTimeset Settings - Choose when you want the different changes of the day to be.", MessageType.Info, true);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Game starting hour: ");
        cl.GetSet_iStartHour = EditorGUILayout.IntSlider("", cl.GetSet_iStartHour, 0, 24, GUILayout.MaxWidth(iMaxWidth));
        GUILayout.EndHorizontal();

        EditorGUILayout.HelpBox(("Timeset settings"), MessageType.None, true);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Sunrise starts at: ");
        cl.GetSet_iSunriseStart = EditorGUILayout.IntSlider("", cl.GetSet_iSunriseStart, 0, 24, GUILayout.MaxWidth(iMaxWidth));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Day starts at: ");
        cl.GetSet_iDayStart = EditorGUILayout.IntSlider("", cl.GetSet_iDayStart, 0, 24, GUILayout.MaxWidth(iMaxWidth));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Sunset starts at: ");
        cl.GetSet_iSunsetStart = EditorGUILayout.IntSlider("", cl.GetSet_iSunsetStart, 0, 24, GUILayout.MaxWidth(iMaxWidth));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Night starts at: ");
        cl.GetSet_iNightStart = EditorGUILayout.IntSlider("", cl.GetSet_iNightStart, 0, 24, GUILayout.MaxWidth(iMaxWidth));
        GUILayout.EndHorizontal();

        EditorGUILayout.HelpBox("Environment settings - day dung chung, khong phu thuoc Sun hay Rain", MessageType.None, true);
        if (bShowTips == true)
            EditorGUILayout.HelpBox("Sun va Rain se cung doc gia tri goc tu day, sau do ap modifier rieng cua tung loai thoi tiet len tren.", MessageType.Info, true);

        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_envSunrise"), new GUIContent("Sunrise Environment"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_envDay"), new GUIContent("Day Environment"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_envSunset"), new GUIContent("Sunset Environment"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_envNight"), new GUIContent("Night Environment"), true);
        serializedObject.ApplyModifiedProperties();
    }
}