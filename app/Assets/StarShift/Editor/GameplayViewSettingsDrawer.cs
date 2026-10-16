using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using StarShift.Gameplay;

namespace StarShift.Editor
{
    [CustomPropertyDrawer(typeof(GameplayViewSettings))]
    public sealed class GameplayViewSettingsDrawer : PropertyDrawer
    {
        private static readonly string[] RegionFields = { "x", "y", "width", "height" };
        private static readonly string[] RegionLabels = { "X", "Y", "Width", "Height" };
        private const string CommitHint = "Nhấn Enter hoặc chuyển ô để áp giá trị.";

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new Foldout { text = property.displayName, value = property.isExpanded };
            root.RegisterValueChangedCallback(change => property.isExpanded = change.newValue);
            root.Add(DelayedField(property.FindPropertyRelative("orthographicSize"), "Orthographic Size"));
            var region = property.FindPropertyRelative("playableViewport");
            var group = new VisualElement();
            group.Add(new Label("Playable Viewport"));
            for (int i = 0; i < RegionFields.Length; i++)
                group.Add(DelayedField(region.FindPropertyRelative(RegionFields[i]), RegionLabels[i]));
            root.Add(group);
            return root;
        }

        private static FloatField DelayedField(SerializedProperty property, string label)
        {
            // Không publish giá trị tạm 0/rỗng để OnValidate ghi đè field đang gõ.
            var field = new FloatField(label) { isDelayed = true, tooltip = CommitHint, name = property.propertyPath };
            field.AddToClassList(BaseField<float>.alignedFieldUssClassName);
            field.BindProperty(property);
            return field;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * (property.isExpanded ? 7 : 1)
                - EditorGUIUtility.standardVerticalSpacing;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var row = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(row, property.isExpanded, label, true);
            if (property.isExpanded)
            {
                var indent = EditorGUI.indentLevel;
                EditorGUI.indentLevel++;
                NextRow(ref row);
                DrawDelayed(row, property.FindPropertyRelative("orthographicSize"), "Orthographic Size");
                NextRow(ref row);
                EditorGUI.LabelField(row, "Playable Viewport");
                EditorGUI.indentLevel++;
                var region = property.FindPropertyRelative("playableViewport");
                for (int i = 0; i < RegionFields.Length; i++)
                {
                    NextRow(ref row);
                    DrawDelayed(row, region.FindPropertyRelative(RegionFields[i]), RegionLabels[i]);
                }
                EditorGUI.indentLevel = indent;
            }
            EditorGUI.EndProperty();
        }

        private static void DrawDelayed(Rect row, SerializedProperty property, string label)
        {
            EditorGUI.BeginChangeCheck();
            var value = EditorGUI.DelayedFloatField(row, new GUIContent(label, CommitHint), property.floatValue);
            if (EditorGUI.EndChangeCheck()) property.floatValue = value;
        }

        private static void NextRow(ref Rect row)
            => row.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
    }
}
