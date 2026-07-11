using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(Weather_Rain))]
public class WeatherRain_Editor : Editor
{
    public bool bShowTips;
    private int iMinWidth = 30;
    private int iMaxWidth = 150;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Show more information");
        bShowTips = EditorGUILayout.Toggle(bShowTips, GUILayout.MaxWidth(iMinWidth));
        GUILayout.EndHorizontal();

        EditorGUILayout.HelpBox("Particle settings", MessageType.None, true);
        if (bShowTips)
        {
            EditorGUILayout.HelpBox("Add your rain particle prefab to this slot.", MessageType.Info, true);
        }

        SerializedProperty rainParticleProp = serializedObject.FindProperty("_gPartRain");

        GUILayout.BeginHorizontal();
        GUILayout.Label("Rain particles (Gameobject): ");
        if (rainParticleProp != null)
        {
            EditorGUILayout.PropertyField(rainParticleProp, GUIContent.none, GUILayout.MaxWidth(iMaxWidth));
        }
        GUILayout.EndHorizontal();

        EditorGUILayout.HelpBox("Fade settings", MessageType.None, true);
        if (bShowTips)
            EditorGUILayout.HelpBox("Fade Time la dong ho duy nhat dieu khien qua trinh chuyen thoi tiet: moi truong, particle va am thanh deu dung chung gia tri nay.", MessageType.Info, true);

        DrawPropertiesExcluding(serializedObject, "m_Script", "_gPartRain");

        serializedObject.ApplyModifiedProperties();
        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }
    }
}