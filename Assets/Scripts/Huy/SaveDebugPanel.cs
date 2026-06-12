using UnityEngine;

namespace SimpleSurvival.SaveLoad
{
    public sealed class SaveDebugPanel : MonoBehaviour
    {
        [SerializeField] private SaveService saveService;

        private string lastResult = "-";

        private void OnGUI()
        {
            using (new GUILayout.AreaScope(new Rect(Screen.width - 240 - 12, 12, 240, 320), "Save Debug", GUI.skin.window))
            {
                GUILayout.Label($"Has save: {saveService.HasSave}");
                GUILayout.Label($"Active: {saveService.IsActive}");
                GUILayout.Label($"Map: {saveService.CurrentMapId}");
                GUILayout.Label($"Last: {lastResult}");

                if (GUILayout.Button("Save"))
                    lastResult = saveService.Save() ? "Saved OK" : "Save FAILED";

                if (GUILayout.Button("Load"))
                    lastResult = saveService.Load() != null ? "Loaded OK" : "No save found";

                if (GUILayout.Button("Delete Save"))
                {
                    saveService.DeleteSave();
                    lastResult = "Deleted";
                }

                if (GUILayout.Button("Toggle Active"))
                    saveService.IsActive = !saveService.IsActive;

                if (GUILayout.Button("Print Path"))
                {
                    Debug.Log(Application.persistentDataPath);
                    lastResult = "Path in Console";
                }
            }
        }
    }
}