using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using PrismZone.Core;

namespace PrismZone.EditorTools
{
    /// <summary>
    /// Property drawer for [NodeIdDropdown] string fields. Pulls the list of
    /// dialogue ids from <see cref="TextTable"/> at draw time. Falls back to a
    /// plain text field if the table isn't loaded yet (e.g. before first import).
    /// </summary>
    [CustomPropertyDrawer(typeof(NodeIdDropdownAttribute))]
    public class NodeIdDropdownDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var ids = LoadDialogueIds();
            if (ids.Length == 0)
            {
                EditorGUI.HelpBox(position, "No dialogue ids — run Tools > Prism Zone > Import Text Table.", MessageType.Info);
                return;
            }

            // Show "<empty>" sentinel as first entry so designer can clear the value.
            var options = new List<string> { "<empty>" };
            options.AddRange(ids);

            int idx = string.IsNullOrEmpty(property.stringValue) ? 0 : options.IndexOf(property.stringValue);
            if (idx < 0)
            {
                // Value present in serialized data but missing from current table.
                // Append it so designer sees it (and can fix), instead of silently changing it.
                options.Add($"{property.stringValue} (missing)");
                idx = options.Count - 1;
            }

            int newIdx = EditorGUI.Popup(position, label.text, idx, options.ToArray());
            if (newIdx != idx)
            {
                property.stringValue = newIdx == 0 ? string.Empty : options[newIdx];
            }
        }

        private static string[] LoadDialogueIds()
        {
            // TextTable.GetAllIds is editor-safe (no play-mode requirements).
            var list = TextTable.GetAllIds(TextEntry.Category.dialogue).ToList();
            list.Sort(System.StringComparer.Ordinal);
            return list.ToArray();
        }
    }
}
