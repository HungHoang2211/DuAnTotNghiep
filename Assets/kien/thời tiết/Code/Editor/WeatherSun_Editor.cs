using UnityEngine;
using UnityEditor;
using System.Collections;

[ExecuteInEditMode]
[CustomEditor(typeof(Weather_Sun))]
public class WeatherSun_Editor : Editor
{
    public bool bShowTips;

    private int iMinWidth = 30;
    private int iMedWidth = 90;
    private int iMaxWidth = 150;

    public override void OnInspectorGUI()
    {
        DrawNewGUI();
        EditorUtility.SetDirty(target);
    }

    private void DrawNewGUI()
    {
        Weather_Sun cl = target as Weather_Sun;

        /* ----- TIPS SETTINGS ----- */
        GUILayout.BeginHorizontal();
        GUILayout.Label("Show more information");
        bShowTips = EditorGUILayout.Toggle(bShowTips, GUILayout.MaxWidth(iMinWidth));
        GUILayout.EndHorizontal();

        /* ----- FADE SETTINGS ----- */
        EditorGUILayout.HelpBox(("Fade settings"), MessageType.None, true);
        if (bShowTips == true)
            EditorGUILayout.HelpBox(("Fade settings - Configure how smooth the weather transitions are for Sunrise, Day, Sunset, and Night."), MessageType.Info, true);
    }
}