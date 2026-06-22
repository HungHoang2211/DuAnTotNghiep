using UnityEngine;
using UnityEditor;
using System.Collections;

[ExecuteInEditMode]
[CustomEditor(typeof(Weather_Rain))]
public class WeatherRain_Editor : Editor
{
    public bool bShowTips;

    private int iMinWidth = 30;
    private int iMaxWidth = 150;

    public override void OnInspectorGUI()
    {
        // Đồng bộ hóa dữ liệu từ file Weather_Rain lên Editor
        serializedObject.Update();

        DrawNewGUI();

        // Lưu lại mọi thay đổi do người dùng chỉnh sửa trên Inspector
        serializedObject.ApplyModifiedProperties();
        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }
    }

    private void DrawNewGUI()
    {
        // Tìm và lấy thuộc tính của biến particle bằng con đường Serialized của Unity Editor
        // Cách này an toàn tuyệt đối, không sợ bị sai lệch Getter/Setter cũ
        SerializedProperty rainParticleProp = serializedObject.FindProperty("_gPartRain");
        if (rainParticleProp == null)
        {
            // Dự phòng trường hợp biến trong file gốc không có dấu gạch dưới (_)
            rainParticleProp = serializedObject.FindProperty("gPartRain");
        }

        /* ----- TIPS SETTINGS ----- */
        GUILayout.BeginHorizontal();
        GUILayout.Label("Show more information");
        bShowTips = EditorGUILayout.Toggle(bShowTips, GUILayout.MaxWidth(iMinWidth));
        GUILayout.EndHorizontal();

        /* ----- PARTICLE SETTINGS ----- */
        EditorGUILayout.HelpBox("Particle settings", MessageType.None, true);
        if (bShowTips)
        {
            EditorGUILayout.HelpBox("Add your rain particle prefab to this slot.", MessageType.Info, true);
        }

        GUILayout.BeginHorizontal();
        GUILayout.Label("Rain particles (Gameobject): ");

        if (rainParticleProp != null)
        {
            // Vẽ ô kéo thả Gameobject thông qua SerializedProperty chuyên dụng
            EditorGUILayout.PropertyField(rainParticleProp, GUIContent.none, GUILayout.MaxWidth(iMaxWidth));
        }
        else
        {
            EditorGUILayout.LabelField("Variable '_gPartRain' not found in script!", EditorStyles.boldLabel);
        }

        GUILayout.EndHorizontal();
    }
}