using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SimpleSurvival.Building.EditorTools
{
    [CustomEditor(typeof(BuildingDatabase))]
    public sealed class BuildingDatabaseEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            if (GUILayout.Button("Rebuild From Project"))
                Rebuild((BuildingDatabase)target);
        }

        private void Rebuild(BuildingDatabase database)
        {
            BuildingData[] found = BuildingAssetFinder.FindAll();
            HashSet<BuildingData> foundSet = new HashSet<BuildingData>(found);

            List<BuildingData> merged = database.Buildings
                .Where(building => building != null && foundSet.Contains(building))
                .ToList();

            HashSet<BuildingData> mergedSet = new HashSet<BuildingData>(merged);
            foreach (BuildingData building in found)
            {
                if (!mergedSet.Contains(building))
                    merged.Add(building);
            }

            database.SetBuildings(merged);
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            Debug.Log($"Building Database rebuilt with {merged.Count} building(s).", database);
        }
    }
}