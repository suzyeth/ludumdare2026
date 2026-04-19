using UnityEngine;
using UnityEngine.UI;
using PrismZone.Core;

namespace PrismZone.UI
{
    /// <summary>
    /// Bottom-left filter indicator. Single Image that swaps sprite based on the
    /// current FilterManager state (None / Red / Green). v1.2 dropped Blue from the
    /// player-facing lens rotation; the field stays for legacy prefabs but is not
    /// part of the cycle. No text label — the icon is the full affordance.
    /// </summary>
    public class FilterHUD : MonoBehaviour
    {
        [Header("Single-Icon Mode")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Sprite iconNone;
        [SerializeField] private Sprite iconRed;
        [SerializeField] private Sprite iconGreen;

        private void OnEnable()
        {
            FilterManager.OnFilterChanged += HandleChange;
            Apply(FilterManager.Instance != null ? FilterManager.Instance.Current : FilterColor.None);
        }

        private void OnDisable() { FilterManager.OnFilterChanged -= HandleChange; }

        private void HandleChange(FilterColor prev, FilterColor next) => Apply(next);

        private void Apply(FilterColor c)
        {
            if (iconImage == null) return;
            iconImage.sprite = c switch
            {
                FilterColor.Red   => iconRed   ?? iconNone,
                FilterColor.Green => iconGreen ?? iconNone,
                _                 => iconNone
            };
        }
    }
}
