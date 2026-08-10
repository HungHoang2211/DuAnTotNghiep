#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SimpleSurvival.Stats;
using SimpleSurvival.Targets;

namespace SimpleSurvival.EditorTools
{
    public static class HarvestNodeIdAssigner
    {
        [MenuItem("Simple Survival/World Objects/Enable Persistence For Harvest + Pickup In Scene")]
        private static void EnablePersistenceForScene()
        {
            string sceneName = EditorSceneManager.GetActiveScene().name;

            bool confirmed = EditorUtility.DisplayDialog(
                "Xác nhận",
                $"Bật persist cho TOÀN BỘ HarvestStats + PickupTarget trong scene '{sceneName}'.\n\n" +
                "CHỈ chạy lệnh này trên scene BaseMap.\n\nTiếp tục?",
                "Tiếp tục", "Huỷ");

            if (!confirmed) return;

            HashSet<string> existingIds = new HashSet<string>();

            HarvestStats[] harvestNodes = Object.FindObjectsByType<HarvestStats>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            PickupTarget[] pickups = Object.FindObjectsByType<PickupTarget>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            int changed = 0;
            changed += ProcessBatch(harvestNodes, "nodeId", existingIds);
            changed += ProcessBatch(pickups, "pickupId", existingIds);

            if (changed > 0)
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log($"[HarvestNodeIdAssigner] Đã xử lý {changed} object " +
                $"(Harvest: {harvestNodes.Length}, Pickup: {pickups.Length}) trong '{sceneName}'. Nhớ Ctrl+S.");
        }

        private static int ProcessBatch(Object[] objects, string idField, HashSet<string> existingIds)
        {
            int count = 0;
            foreach (Object obj in objects)
            {
                SerializedObject so = new SerializedObject(obj);
                SerializedProperty persistProp = so.FindProperty("persistAcrossSessions");
                SerializedProperty idProp = so.FindProperty(idField);
                if (persistProp == null || idProp == null) continue;

                bool changed = false;

                if (!persistProp.boolValue) { persistProp.boolValue = true; changed = true; }

                if (string.IsNullOrWhiteSpace(idProp.stringValue))
                {
                    string newId = GenerateUniqueId(obj.name, existingIds);
                    idProp.stringValue = newId;
                    existingIds.Add(newId);
                    changed = true;
                }

                if (changed)
                {
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(obj);
                    count++;
                }
            }
            return count;
        }

        private static string GenerateUniqueId(string baseName, HashSet<string> existingIds)
        {
            string cleanName = baseName.Replace(" ", "").Replace("(Clone)", "");
            string id;
            do
            {
                string suffix = System.Guid.NewGuid().ToString("N").Substring(0, 6);
                id = $"{cleanName}_{suffix}";
            } while (existingIds.Contains(id));
            return id;
        }
    }
}
#endif