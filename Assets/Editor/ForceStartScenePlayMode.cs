using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SimpleSurvival.EditorTools
{
    [InitializeOnLoad]
    public static class ForceStartScenePlayMode
    {
        private const string StartScenePath = "Assets/Scenes/Game/Start.unity";
        private const string PrefKey = "SimpleSurvival_ForceStartScenePlayMode";

        static ForceStartScenePlayMode()
        {
            ApplySetting();
        }

        [MenuItem("Simple Survival/Force Play From Start", false, 1)]
        private static void ToggleForceStartScene()
        {
            SetEnabled(!IsEnabled());
        }

        [MenuItem("Simple Survival/Force Play From Start", true)]
        private static bool ToggleForceStartSceneValidate()
        {
            Menu.SetChecked("Simple Survival/Force Play From Start", IsEnabled());
            return true;
        }

        private static bool IsEnabled()
        {
            return EditorPrefs.GetBool(PrefKey, true);
        }

        private static void SetEnabled(bool enabled)
        {
            EditorPrefs.SetBool(PrefKey, enabled);
            ApplySetting();
        }

        private static void ApplySetting()
        {
            EditorSceneManager.playModeStartScene = IsEnabled()
                ? AssetDatabase.LoadAssetAtPath<SceneAsset>(StartScenePath)
                : null;
        }
    }
}