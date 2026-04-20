using System.Collections.Generic;
using UnityEngine;
using PrismZone.Core;

namespace PrismZone.Interact
{
    /// <summary>
    /// Toggles visibility of an object based on GameFlags state. Visible while
    /// every key in <see cref="requireFlags"/> is set AND every key in
    /// <see cref="forbidFlags"/> is unset. Mirrors <see cref="GlassesVisibility"/>
    /// but listens on <see cref="GameFlags.OnChanged"/> instead of FilterManager.
    /// </summary>
    [AddComponentMenu("Prism Zone/Flag Visibility")]
    public class FlagVisibility : MonoBehaviour
    {
        public enum Mode { SpriteOnly, GameObject, SpriteAndCollider }

        [Header("Visibility Flags")]
        [Tooltip("All of these flags must be set for the object to be visible.")]
        [SerializeField] private List<string> requireFlags = new();
        [Tooltip("If any of these flags are set, the object is hidden.")]
        [SerializeField] private List<string> forbidFlags = new();

        [Header("Mode")]
        [SerializeField] private Mode mode = Mode.SpriteAndCollider;

        [Header("Targets (auto-found if empty)")]
        [SerializeField] private SpriteRenderer[] sprites;
        [SerializeField] private Collider2D[] colliders;

        private void Awake()
        {
            if (sprites == null || sprites.Length == 0) sprites = GetComponentsInChildren<SpriteRenderer>(true);
            if (colliders == null || colliders.Length == 0) colliders = GetComponentsInChildren<Collider2D>(true);
        }

        private void OnEnable()
        {
            GameFlags.OnChanged += HandleFlagChanged;
            Apply(Evaluate());
        }

        private void OnDisable()
        {
            GameFlags.OnChanged -= HandleFlagChanged;
        }

        private void HandleFlagChanged(string key) => Apply(Evaluate());

        private bool Evaluate()
        {
            if (requireFlags != null && !GameFlags.AllSet(requireFlags)) return false;
            if (forbidFlags != null && GameFlags.AnySet(forbidFlags)) return false;
            return true;
        }

        private void Apply(bool visible)
        {
            switch (mode)
            {
                case Mode.SpriteOnly:
                    foreach (var sr in sprites) if (sr != null) sr.enabled = visible;
                    break;
                case Mode.GameObject:
                    if (gameObject.activeSelf != visible) gameObject.SetActive(visible);
                    break;
                case Mode.SpriteAndCollider:
                    foreach (var sr in sprites) if (sr != null) sr.enabled = visible;
                    foreach (var col in colliders) if (col != null) col.enabled = visible;
                    break;
            }
        }

        public bool IsCurrentlyVisible => Evaluate();
    }
}
