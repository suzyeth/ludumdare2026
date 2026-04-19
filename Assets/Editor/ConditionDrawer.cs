using UnityEditor;
using UnityEngine;
using PrismZone.Core;

namespace PrismZone.EditorTools
{
    /// <summary>
    /// Inspector for the <see cref="Condition"/> struct (FRAMEWORK.md §6.4).
    /// Renders the three string arrays as expandable lists. The default array
    /// drawer already gives autocomplete via the [FlagKeyDropdown] attribute
    /// on each element when the list is annotated — for now we just rely on the
    /// foldout to keep the inspector compact.
    /// </summary>
    [CustomPropertyDrawer(typeof(Condition))]
    public class ConditionDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

            if (!property.isExpanded) { EditorGUI.EndProperty(); return; }

            EditorGUI.indentLevel++;
            var require = property.FindPropertyRelative("requireAll");
            var forbid  = property.FindPropertyRelative("forbidAll");
            var any     = property.FindPropertyRelative("requireAny");

            float y = position.y + EditorGUIUtility.singleLineHeight + 2f;
            y = DrawArray(position.x, y, position.width, require, "Require All (AND)");
            y = DrawArray(position.x, y, position.width, forbid,  "Forbid All (NAND)");
            y = DrawArray(position.x, y, position.width, any,     "Require Any (OR, optional)");
            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float h = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded) return h;
            h += 2f;
            h += ArrayHeight(property.FindPropertyRelative("requireAll"));
            h += ArrayHeight(property.FindPropertyRelative("forbidAll"));
            h += ArrayHeight(property.FindPropertyRelative("requireAny"));
            return h;
        }

        private static float DrawArray(float x, float y, float width, SerializedProperty arr, string label)
        {
            float lh = EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(new Rect(x, y, width, EditorGUI.GetPropertyHeight(arr, true)),
                                    arr, new GUIContent(label), true);
            return y + EditorGUI.GetPropertyHeight(arr, true) + 2f;
        }

        private static float ArrayHeight(SerializedProperty arr)
            => EditorGUI.GetPropertyHeight(arr, true) + 2f;
    }
}
