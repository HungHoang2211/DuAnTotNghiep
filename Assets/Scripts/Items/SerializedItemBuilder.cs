#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace SimpleSurvival.Items.Tests
{
    /// <summary>
    /// Test-only helper. ItemData fields are private [SerializeField] because
    /// designers author items as assets in the Inspector, not via code. Tests
    /// still need to build items with known values, so this writes those fields
    /// through SerializedObject — the same mechanism the Inspector uses.
    ///
    /// Editor-only: SerializedObject is not available in player builds, so this
    /// entire file is wrapped in #if UNITY_EDITOR and never ships in the game.
    /// </summary>
    public static class SerializedItemBuilder
    {
        public static void Configure(ItemData item, bool isStackable, int maxStack,
            int maxDurability)
        {
            SerializedObject serialized = new SerializedObject(item);

            serialized.FindProperty("isStackable").boolValue = isStackable;
            serialized.FindProperty("maxStack").intValue = maxStack;
            serialized.FindProperty("maxDurability").intValue = maxDurability;

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif