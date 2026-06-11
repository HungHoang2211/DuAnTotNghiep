using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SimpleSurvival.Items.EditorTools
{
    [CustomEditor(typeof(ItemDatabase))]
    public sealed class ItemDatabaseEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            if (GUILayout.Button("Rebuild From Project"))
                Rebuild((ItemDatabase)target);
        }

        private void Rebuild(ItemDatabase database)
        {
            ItemData[] all = ItemAssetFinder.FindAll()
                .OrderBy(item => item.ItemId)
                .ToArray();

            database.SetItems(all);
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            Debug.Log($"Item Database rebuilt with {all.Length} item(s).", database);
        }
    }
}