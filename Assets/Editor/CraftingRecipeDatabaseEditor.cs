using System.Collections.Generic;
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
            CraftingRecipeData[] found = CraftingRecipeAssetFinder.FindAll();
            HashSet<CraftingRecipeData> foundSet = new HashSet<CraftingRecipeData>(found);

            List<CraftingRecipeData> merged = database.Recipes
                .Where(recipe => recipe != null && foundSet.Contains(recipe))
                .ToList();

            HashSet<CraftingRecipeData> mergedSet = new HashSet<CraftingRecipeData>(merged);
            foreach (CraftingRecipeData recipe in found)
            {
                if (!mergedSet.Contains(recipe))
                    merged.Add(recipe);
            }

            database.SetRecipes(merged);
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            Debug.Log($"Crafting Recipe Database rebuilt with {merged.Count} recipe(s).", database);
        }
    }
}