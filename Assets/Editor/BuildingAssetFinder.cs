#if UNITY_EDITOR
using System.Linq;
using UnityEditor;

namespace SimpleSurvival.Building.EditorTools
{
    public static class BuildingAssetFinder
    {
        public static BuildingData[] FindAll()
        {
            return AssetDatabase.FindAssets("t:BuildingData")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<BuildingData>)
                .Where(building => building != null)
                .ToArray();
        }
    }
}
#endif