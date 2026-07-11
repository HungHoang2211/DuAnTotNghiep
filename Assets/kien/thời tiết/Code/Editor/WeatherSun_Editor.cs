using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(Weather_Sun))]
public class WeatherSun_Editor : Editor
{
    public bool bShowTips;
    private int iMinWidth = 30;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Show more information");
        bShowTips = EditorGUILayout.Toggle(bShowTips, GUILayout.MaxWidth(iMinWidth));
        GUILayout.EndHorizontal();

        EditorGUILayout.HelpBox("Fade settings", MessageType.None, true);
        if (bShowTips)
            EditorGUILayout.HelpBox("Fade Time la dong ho duy nhat dieu khien qua trinh chuyen thoi tiet: moi truong, particle va am thanh deu dung chung gia tri nay.", MessageType.Info, true);

        DrawDefaultInspector();

        serializedObject.ApplyModifiedProperties();
        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }
    }
}