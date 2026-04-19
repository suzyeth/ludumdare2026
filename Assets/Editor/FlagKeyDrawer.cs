using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using PrismZone.Core;

namespace PrismZone.EditorTools
{
    /// <summary>
    /// Property drawer for [FlagKeyDropdown] string fields. Pulls every const string
    /// from FlagKeys (both partials — auto-generated + handwritten built-in) via
    /// reflection so designers don't have to remember key names.
    /// </summary>
    [CustomPropertyDrawer(typeof(FlagKeyDropdownAttribute))]
    public class FlagKeyDropdownDrawer : PropertyDrawer
    {
        private static string[] _cachedKeys;
        private static double _cacheBuiltAt;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var keys = GetCachedKeys();
            if (keys.Length == 0)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var options = new List<string> { "<empty>" };
            options.AddRange(keys);

            int idx = string.IsNullOrEmpty(property.stringValue) ? 0 : options.IndexOf(property.stringValue);
            if (idx < 0)
            {
                options.Add($"{property.stringValue} (unknown)");
                idx = options.Count - 1;
            }

            int newIdx = EditorGUI.Popup(position, label.text, idx, options.ToArray());
            if (newIdx != idx)
            {
                property.stringValue = newIdx == 0 ? string.Empty : options[newIdx];
            }
        }

        private static string[] GetCachedKeys()
        {
            // Re-scan once a second so a re-import refreshes the dropdown without
            // a domain reload, but doesn't hammer reflection on every paint.
            if (_cachedKeys != null && EditorApplication.timeSinceStartup - _cacheBuiltAt < 1.0)
                return _cachedKeys;

            var keys = new SortedSet<string>(StringComparer.Ordinal);
            CollectConstStrings(typeof(FlagKeys), keys);
            _cachedKeys = keys.ToArray();
            _cacheBuiltAt = EditorApplication.timeSinceStartup;
            return _cachedKeys;
        }

        private static void CollectConstStrings(Type t, SortedSet<string> bag)
        {
            // Const string fields directly on this class.
            foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
            {
                if (!f.IsLiteral || f.IsInitOnly) continue;
                if (f.FieldType != typeof(string)) continue;
                var v = f.GetRawConstantValue() as string;
                if (!string.IsNullOrEmpty(v)) bag.Add(v);
            }
            // Nested static classes (Dialogue / Inventory / Filter / Broadcast / etc.)
            foreach (var nested in t.GetNestedTypes(BindingFlags.Public | BindingFlags.Static))
            {
                CollectConstStrings(nested, bag);
            }
        }
    }
}
