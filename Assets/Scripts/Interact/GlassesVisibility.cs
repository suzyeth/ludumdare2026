using System.Collections.Generic;
using UnityEngine;
using PrismZone.Core;

namespace PrismZone.Interact
{
    /// <summary>
    /// Toggles visibility of an object based on the current FilterManager filter.
    /// Designer configures one or more <see cref="visibleWhen"/> filters; the GO
    /// becomes visible when the active filter matches any of them.
    ///
    /// v1.2 visibility table from spec §4.2:
    ///   - 4F楼梯门      visibleWhen=[Red, Green]   (none = invisible)
    ///   - 嘲笑纸条      visibleWhen=[Red]          (only red lens reveals)
    ///   - 301暗格标记   visibleWhen=[Red]
    ///   - 402教室门     visibleWhen=[Red]
    ///   - 血黑板内容    visibleWhen=[Red]
    ///   - 1F出口箭头    visibleWhen=[Green]
    ///   - 录音笔        visibleWhen=[None, Red, Green] (always — set unmanaged or list all)
    ///
    /// Three modes for what "visible" means (use whichever the prop needs):
    ///   - SpriteOnly  : toggles SpriteRenderer.enabled (keeps GO + colliders alive)
    ///   - GameObject  : SetActive(false) — also disables children + colliders + scripts
    ///   - SpriteAndCollider : sprite + each Collider2D enabled flag (keeps scripts alive)
    /// </summary>
    [AddComponentMenu("Prism Zone/Glasses Visibility")]
    public class GlassesVisibility : MonoBehaviour
    {
        public enum Mode { SpriteOnly, GameObject, SpriteAndCollider }

        [Header("Visibility")]
        [Tooltip("Filter colors that reveal this object. Empty list = always visible (no-op).")]
        [SerializeField] private List<FilterColor> visibleWhen = new() { FilterColor.None };

        [Header("Mode")]
        [SerializeField] private Mode mode = Mode.SpriteOnly;

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
            FilterManager.OnFilterChanged += HandleFilterChanged;
            ApplyForCurrentFilter();
        }

        private void OnDisable()
        {
            FilterManager.OnFilterChanged -= HandleFilterChanged;
        }

        private void HandleFilterChanged(FilterColor prev, FilterColor next) => Apply(next);

        private void ApplyForCurrentFilter()
        {
            var c = FilterManager.Instance != null ? FilterManager.Instance.Current : FilterColor.None;
            Apply(c);
        }

        private void Apply(FilterColor c)
        {
            bool visible = visibleWhen == null || visibleWhen.Count == 0 || visibleWhen.Contains(c);

            switch (mode)
            {
                case Mode.SpriteOnly:
                    foreach (var sr in sprites) if (sr != null) sr.enabled = visible;
                    break;
                case Mode.GameObject:
                    // Avoid stomping the very component that drives this — toggle children only
                    // when the host transform itself should hide.
                    if (gameObject.activeSelf != visible) gameObject.SetActive(visible);
                    break;
                case Mode.SpriteAndCollider:
                    foreach (var sr in sprites) if (sr != null) sr.enabled = visible;
                    foreach (var col in colliders) if (col != null) col.enabled = visible;
                    break;
            }
        }

        /// <summary>True if the object currently passes its visibility gate.</summary>
        public bool IsCurrentlyVisible
        {
            get
            {
                var c = FilterManager.Instance != null ? FilterManager.Instance.Current : FilterColor.None;
                return visibleWhen == null || visibleWhen.Count == 0 || visibleWhen.Contains(c);
            }
        }
    }
}
