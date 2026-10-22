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
        private static readonly string[] RegionLabels = { "X", "Y", "Chiều rộng", "Chiều cao" };
        private const string ZoomLabel = "Độ thu phóng (Size)";
        private const string ZoomHint = "Tăng Size: nhìn rộng hơn, tàu nhỏ hơn. Giảm Size: nhìn gần hơn, tàu lớn hơn. Chiều cao vùng nhìn = Size × 2 đơn vị world; ví dụ Size 6 thấy cao 12 đơn vị. Đây không phải kích thước pixel.";
        private const string CommitHint = "Nhấn Enter hoặc chuyển ô để áp giá trị.";

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new Foldout { text = VietnameseInspectorLabels.Label(property).text, value = property.isExpanded };
            root.RegisterValueChangedCallback(change => property.isExpanded = change.newValue);
            root.Add(DelayedField(property.FindPropertyRelative("orthographicSize"), ZoomLabel));
            var region = property.FindPropertyRelative("playableViewport");
            var group = new VisualElement();
            group.Add(new Label("Vùng di chuyển (0–1)"));
            for (int i = 0; i < RegionFields.Length; i++)
                group.Add(DelayedField(region.FindPropertyRelative(RegionFields[i]), RegionLabels[i]));
            root.Add(group);
            return root;
        }

        private static FloatField DelayedField(SerializedProperty property, string label)
        {
            // Không publish giá trị tạm 0/rỗng để OnValidate ghi đè field đang gõ.
            var field = new FloatField(label) { isDelayed = true, tooltip = Hint(property), name = property.propertyPath };
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
                DrawDelayed(row, property.FindPropertyRelative("orthographicSize"), ZoomLabel);
                NextRow(ref row);
                EditorGUI.LabelField(row, "Vùng di chuyển (0–1)");
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
            var value = EditorGUI.DelayedFloatField(row, new GUIContent(label, Hint(property)), property.floatValue);
            if (EditorGUI.EndChangeCheck()) property.floatValue = value;
        }

        private static string Hint(SerializedProperty property)
            => (property.name == "orthographicSize" ? ZoomHint : "Vùng chuẩn hóa: X/Y là góc dưới trái; chiều rộng/cao là phần màn hình cho phép tàu di chuyển. 1 = toàn bộ trục, 0.5 = một nửa.") + "\n" + CommitHint;

        private static void NextRow(ref Rect row)
            => row.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
    }
}
