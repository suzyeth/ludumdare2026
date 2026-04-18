using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PrismZone.Core;

namespace PrismZone.UI
{
    /// <summary>
    /// Bottom-left filter indicator. One dot per color; the active color is opaque,
    /// others muted. Optional label for accessibility / text-heavy UI.
    /// </summary>
    public class FilterHUD : MonoBehaviour
    {
        [SerializeField] private Image noneDot;
        [SerializeField] private Image redDot;
        [SerializeField] private Image greenDot;
        [SerializeField] private Image blueDot;
        [SerializeField] private TMP_Text label;
        [SerializeField, Range(0f, 1f)] private float mutedAlpha = 0.25f;
        [SerializeField, Range(0f, 1f)] private float activeAlpha = 1.0f;

        private void OnEnable()
        {
            FilterManager.OnFilterChanged += HandleChange;
            RefreshFromManager();
        }

        private void OnDisable()
        {
            FilterManager.OnFilterChanged -= HandleChange;
        }

        private void HandleChange(FilterColor prev, FilterColor next) => Apply(next);

        private void RefreshFromManager()
        {
            Apply(FilterManager.Instance != null ? FilterManager.Instance.Current : FilterColor.None);
        }

        private void Apply(FilterColor c)
        {
            SetAlpha(noneDot,  c == FilterColor.None);
            SetAlpha(redDot,   c == FilterColor.Red);
            SetAlpha(greenDot, c == FilterColor.Green);
            SetAlpha(blueDot,  c == FilterColor.Blue);
            if (label != null)
            {
                string key = c switch
                {
                    FilterColor.Red   => "ui.filter.red",
                    FilterColor.Green => "ui.filter.green",
                    FilterColor.Blue  => "ui.filter.blue",
                    _                 => "ui.filter.none"
                };
                label.text = I18nManager.Get(key);
            }
        }

        private void SetAlpha(Image img, bool active)
        {
            if (img == null) return;
            var col = img.color;
            col.a = active ? activeAlpha : mutedAlpha;
            img.color = col;
        }
    }
}
