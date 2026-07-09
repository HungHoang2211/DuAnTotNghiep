#if UNITY_EDITOR
using System.Linq;
using UnityEditor;

namespace SimpleSurvival.Items.EditorTools
{
    public static class CraftingRecipeAssetFinder
    {
        public static CraftingRecipeData[] FindAll()
        {
            return AssetDatabase.FindAssets("t:CraftingRecipeData")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<CraftingRecipeData>)
                .Where(recipe => recipe != null)
                .ToArray();
        }
    }
}
#endif