using UnityEditor;
using UnityEngine;

namespace SimpleSurvival.Characters.Appearance.Editor
{
    [CustomPropertyDrawer(typeof(BodypartResource))]
    public sealed class BodypartResourceDrawer : PropertyDrawer
    {
        private const float LineHeight = 18f;
        private const float Spacing = 2f;
        private const float ObjectFieldWidth = 140f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            Rect headerRect = new Rect(position.x, position.y, position.width, LineHeight);
            Rect foldoutRect = new Rect(headerRect.x, headerRect.y, headerRect.width - ObjectFieldWidth, headerRect.height);
            Rect objectFieldRect = new Rect(headerRect.xMax - ObjectFieldWidth, headerRect.y, ObjectFieldWidth, headerRect.height);

            GUIContent headerLabel = BuildHeaderLabel(property, label);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, headerLabel, true);
            EditorGUI.ObjectField(objectFieldRect, property, GUIContent.none);

            if (property.isExpanded && property.objectReferenceValue != null)
                DrawNestedFields(position, property);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded || property.objectReferenceValue == null)
                return LineHeight;

            float height = LineHeight + Spacing;
            SerializedObject nestedObject = new SerializedObject(property.objectReferenceValue);
            SerializedProperty iterator = nestedObject.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.name == "m_Script")
                    continue;

                height += EditorGUI.GetPropertyHeight(iterator, true) + Spacing;
            }

            return height;
        }

        private static GUIContent BuildHeaderLabel(SerializedProperty property, GUIContent fieldLabel)
        {
            bool isArrayElement = IsArrayElement(property);
            string prefix = isArrayElement ? ExtractArrayIndex(property) + " " : string.Empty;

            if (property.objectReferenceValue == null)
            {
                string emptyText = isArrayElement ? "(Empty)" : fieldLabel.text;
                return new GUIContent(prefix + emptyText);
            }

            SerializedObject nestedObject = new SerializedObject(property.objectReferenceValue);
            SerializedProperty idProperty = nestedObject.FindProperty("bodypartId");
            string idValue = idProperty != null ? idProperty.stringValue : string.Empty;
            string name = string.IsNullOrEmpty(idValue) ? property.objectReferenceValue.name : idValue;

            return new GUIContent(prefix + name);
        }

        private static bool IsArrayElement(SerializedProperty property)
        {
            return property.propertyPath.EndsWith("]");
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

        private static void DrawNestedFields(Rect position, SerializedProperty property)
        {
            SerializedObject nestedObject = new SerializedObject(property.objectReferenceValue);
            nestedObject.Update();

            float y = position.y + LineHeight + Spacing;
            SerializedProperty iterator = nestedObject.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.name == "m_Script")
                    continue;

                float height = EditorGUI.GetPropertyHeight(iterator, true);
                Rect fieldRect = new Rect(position.x + 15f, y, position.width - 15f, height);
                EditorGUI.PropertyField(fieldRect, iterator, true);
                y += height + Spacing;
            }

            nestedObject.ApplyModifiedProperties();
        }
    }
}