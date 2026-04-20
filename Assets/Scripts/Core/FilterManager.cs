using System;
using UnityEngine;

namespace PrismZone.Core
{
    /// <summary>
    /// Owns the active screen filter. NPCs, shaders, and HUD subscribe to OnFilterChanged.
    /// The actual screen effect is driven by a URP Full Screen Pass Renderer Feature whose
    /// material reads _FilterMode (0=none, 1=red, 2=green, 3=blue) from this manager.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class FilterManager : MonoBehaviour
    {
        public static FilterManager Instance { get; private set; }

        public static event Action<FilterColor, FilterColor> OnFilterChanged;

        [SerializeField] private Material fullScreenMaterial;
        [SerializeField] private string filterModeProperty = "_FilterMode";

        private static readonly int FilterModeId = Shader.PropertyToID("_FilterMode");

        public FilterColor Current { get; private set; } = FilterColor.None;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            PushToShader();
            SyncFlagsToCurrent();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SetFilter(FilterColor next)
        {
            if (next == Current) return;
            var prev = Current;
            Current = next;
            PushToShader();
            SyncFlagsToCurrent();
            OnFilterChanged?.Invoke(prev, next);
        }

        // Mirror Current into GameFlags so DialogueTrigger.condition gates (e.g.
        // "requireAll: filter.current.red" on the Blackboard) can key off filter
        // state. Without this, FlagKeys.Filter.Current_* is never written and any
        // dialogue requiring a lens colour silently blocks forever.
        private void SyncFlagsToCurrent()
        {
            GameFlags.Set(FlagKeys.Filter.Current_None,  Current == FilterColor.None);
            GameFlags.Set(FlagKeys.Filter.Current_Red,   Current == FilterColor.Red);
            GameFlags.Set(FlagKeys.Filter.Current_Green, Current == FilterColor.Green);
        }

        /// <summary>
        /// v1.2 spec: Q cycles None → Red → Green → None. Blue is retained in the
        /// <see cref="FilterColor"/> enum for legacy prefabs (BLUE_Weaver NPC) but
        /// is no longer part of the player-facing lens rotation.
        /// </summary>
        public void CycleLens()
        {
            FilterColor next = Current switch
            {
                FilterColor.None  => FilterColor.Red,
                FilterColor.Red   => FilterColor.Green,
                FilterColor.Green => FilterColor.None,
                _                 => FilterColor.None, // Blue or unknown → reset
            };
            SetFilter(next);
        }

        private void PushToShader()
        {
            if (fullScreenMaterial == null) return;
            int id = string.IsNullOrEmpty(filterModeProperty) ? FilterModeId : Shader.PropertyToID(filterModeProperty);
            // Property is declared as Float in the shader for URP SRP batcher compatibility;
            // SetFloat with an int value casts to int inside the shader.
            fullScreenMaterial.SetFloat(id, (float)Current);
        }
    }
}
