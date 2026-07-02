using UnityEditor;
using UnityEngine;

namespace SimpleSurvival.Characters.Appearance.Editor
{
    [CustomPropertyDrawer(typeof(BodypartSlotEntry))]
    public sealed class BodypartSlotEntryDrawer : PropertyDrawer
    {
        private const float Spacing = 2f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty kindProperty = property.FindPropertyRelative("kind");
            GUIContent headerLabel = BuildHeaderLabel(property, kindProperty);

            float lineHeight = EditorGUIUtility.singleLineHeight;
            Rect foldoutRect = new Rect(position.x, position.y, position.width, lineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, headerLabel, true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                float y = foldoutRect.yMax + Spacing;

                SerializedProperty[] children =
                {
                    kindProperty,
                    property.FindPropertyRelative("atlasRect"),
                    property.FindPropertyRelative("bodyparts")
                };

                foreach (SerializedProperty child in children)
                {
                    float height = EditorGUI.GetPropertyHeight(child, true);
                    Rect fieldRect = new Rect(position.x, y, position.width, height);
                    EditorGUI.PropertyField(fieldRect, child, true);
                    y += height + Spacing;
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded)
                return height;

            height += Spacing;
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("kind"), true) + Spacing;
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("atlasRect"), true) + Spacing;
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("bodyparts"), true) + Spacing;

            return height;
        }

        private static GUIContent BuildHeaderLabel(SerializedProperty property, SerializedProperty kindProperty)
        {
            int index = ExtractArrayIndex(property);
            string kindName = kindProperty.enumDisplayNames[kindProperty.enumValueIndex];
            string prefix = index >= 0 ? index + " " : string.Empty;

            return new GUIContent(prefix + kindName);
        }

        private static int ExtractArrayIndex(SerializedProperty property)
        {
            string path = property.propertyPath;
            int start = path.LastIndexOf('[') + 1;
            int end = path.LastIndexOf(']');

            if (start <= 0 || end <= start)
                return -1;

            return int.TryParse(path.Substring(start, end - start), out int index) ? index : -1;
        }
    }
}