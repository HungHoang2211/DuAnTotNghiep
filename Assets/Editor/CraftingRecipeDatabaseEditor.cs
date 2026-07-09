using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SimpleSurvival.Items.EditorTools
{
    [CustomEditor(typeof(CraftingRecipeDatabase))]
    public sealed class CraftingRecipeDatabaseEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            if (GUILayout.Button("Rebuild From Project"))
                Rebuild((CraftingRecipeDatabase)target);
        }

        private void Rebuild(CraftingRecipeDatabase database)
        {
            CraftingRecipeData[] all = CraftingRecipeAssetFinder.FindAll()
                .OrderBy(recipe => recipe.ResultItem != null ? recipe.ResultItem.ItemName : string.Empty)
                .ToArray();
            database.SetRecipes(all);
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            Debug.Log($"Crafting Recipe Database rebuilt with {all.Length} recipe(s).", database);
        }
    }
}