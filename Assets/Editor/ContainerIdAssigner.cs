#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SimpleSurvival.Loot;

namespace SimpleSurvival.EditorTools
{
    public static class ContainerIdAssigner
    {
        [MenuItem("Simple Survival/Loot/Assign Missing Container IDs")]
        private static void AssignMissingIds()
        {
            LootContainer[] containers = Object.FindObjectsByType<LootContainer>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            HashSet<string> existingIds = new HashSet<string>();
            foreach (LootContainer c in containers)
            {
                if (!string.IsNullOrWhiteSpace(c.ContainerId))
                    existingIds.Add(c.ContainerId);
            }

            int assignedCount = 0;

            foreach (LootContainer c in containers)
            {
                SerializedObject so = new SerializedObject(c);
                SerializedProperty persistProp = so.FindProperty("persistAcrossSessions");
                SerializedProperty idProp = so.FindProperty("containerId");

                if (persistProp == null || idProp == null) continue;
                if (!persistProp.boolValue) continue;
                if (!string.IsNullOrWhiteSpace(idProp.stringValue)) continue;

                string newId = GenerateUniqueId(c.name, existingIds);
                idProp.stringValue = newId;
                so.ApplyModifiedProperties();

                existingIds.Add(newId);
                assignedCount++;
                EditorUtility.SetDirty(c);
            }

            if (assignedCount > 0)
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log($"[ContainerIdAssigner] Đã gán {assignedCount} Container Id mới trong scene '{EditorSceneManager.GetActiveScene().name}'. Nhớ Ctrl+S.");
        }

        private static string GenerateUniqueId(string baseName, HashSet<string> existingIds)
        {
            string cleanName = baseName.Replace(" ", "");
            string id;
            do
            {
                string suffix = System.Guid.NewGuid().ToString("N").Substring(0, 4);
                id = $"{cleanName}_{suffix}";
            } while (existingIds.Contains(id));

            return id;
        }
    }
}
#endif