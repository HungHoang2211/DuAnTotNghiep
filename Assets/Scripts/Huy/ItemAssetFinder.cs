#if UNITY_EDITOR
using System.Linq;
using UnityEditor;

namespace SimpleSurvival.Items.EditorTools
{
    public static class ItemAssetFinder
    {
        public static ItemData[] FindAll()
        {
            return AssetDatabase.FindAssets("t:ItemData")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<ItemData>)
                .Where(item => item != null)
                .ToArray();
        }
    }
}
#endif