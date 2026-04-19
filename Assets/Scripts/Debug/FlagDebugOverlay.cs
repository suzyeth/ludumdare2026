#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PrismZone.DevTools
{
    /// <summary>
    /// F10 toggles a runtime IMGUI overlay listing every <see cref="Core.GameFlags"/>
    /// entry, grouped by namespace prefix (FRAMEWORK.md §6.5).
    ///
    /// Editor-or-development-only — stripped from release builds. Drop on _Bootstrap
    /// (or any always-loaded GO) and forget it; nothing wires to it at runtime.
    /// </summary>
    public class FlagDebugOverlay : MonoBehaviour
    {
        [SerializeField] private Key toggleKey = Key.F10;
        [SerializeField] private bool startVisible = false;
        [SerializeField, Range(120, 600)] private int panelWidth = 360;

        private bool _show;
        private Vector2 _scroll;
        private GUIStyle _boxStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _entryStyle;

        private void Awake() { _show = startVisible; }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb[toggleKey].wasPressedThisFrame) _show = !_show;
        }

        private void OnGUI()
        {
            if (!_show) return;
            EnsureStyles();

            int height = Mathf.Min(Screen.height - 20, 720);
            GUILayout.BeginArea(new Rect(10, 10, panelWidth, height), _boxStyle);

            GUILayout.Label($"GameFlags  ·  F{(int)toggleKey - (int)Key.F1 + 1} to hide", _headerStyle);

            var bools = Core.GameFlags.SnapshotBools();
            var ints  = Core.GameFlags.SnapshotInts();

            if (GUILayout.Button("Clear All Flags")) Core.GameFlags.Clear();
            GUILayout.Space(4);

            _scroll = GUILayout.BeginScrollView(_scroll);

            foreach (var grp in bools.OrderBy(kv => kv.Key)
                                     .GroupBy(kv => kv.Key.Split('.')[0]))
            {
                GUILayout.Label($"▼ {grp.Key}  ({grp.Count()})", _headerStyle);
                foreach (var kv in grp)
                {
                    string mark = kv.Value ? "[x]" : "[ ]";
                    GUILayout.Label($"  {mark}  {kv.Key}", _entryStyle);
                }
            }

            if (ints.Count > 0)
            {
                GUILayout.Space(4);
                GUILayout.Label("▼ ints", _headerStyle);
                foreach (var kv in ints.OrderBy(kv => kv.Key))
                    GUILayout.Label($"  {kv.Value,4} {kv.Key}", _entryStyle);
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (_boxStyle != null) return;
            _boxStyle = new GUIStyle(GUI.skin.box) { padding = new RectOffset(8, 8, 8, 8) };
            _headerStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 12 };
            _entryStyle = new GUIStyle(GUI.skin.label) { fontSize = 11 };
        }
    }
}
#endif
