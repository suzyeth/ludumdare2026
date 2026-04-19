using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PrismZone.Core;

namespace PrismZone.UI
{
    /// <summary>
    /// Bottom-left three-ring filter indicator. Each color ring has separate
    /// _on / _off sprites. On FilterChanged, we swap the sprite of the matching
    /// ring; others show their _off sprite. Alpha is no longer used for state —
    /// hidden "none" case simply shows all three rings in _off state.
    ///
    /// Back-compat: the old noneDot/redDot/greenDot/blueDot Images keep working as
    /// a fallback when ring sprites are not assigned.
    /// </summary>
    public class FilterHUD : MonoBehaviour
    {
        [Header("Ring Images (new, preferred)")]
        [SerializeField] private Image redRing;
        [SerializeField] private Image greenRing;
        [SerializeField] private Image blueRing;

        [Header("Ring Sprites")]
        [SerializeField] private Sprite redOn,   redOff;
        [SerializeField] private Sprite greenOn, greenOff;
        [SerializeField] private Sprite blueOn,  blueOff;

        [Header("Legacy 4-dot mode (fallback)")]
        [SerializeField] private Image noneDot;
        [SerializeField] private Image redDot;
        [SerializeField] private Image greenDot;
        [SerializeField] private Image blueDot;
        [SerializeField, Range(0f, 1f)] private float mutedAlpha = 0.25f;
        [SerializeField, Range(0f, 1f)] private float activeAlpha = 1.0f;

        [Header("Label")]
        [SerializeField] private TMP_Text label;

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
            // Ring mode (preferred if ring Images + sprites are wired)
            if (redRing != null)   redRing.sprite   = (c == FilterColor.Red)   ? (redOn   ?? redOff)   : redOff;
            if (greenRing != null) greenRing.sprite = (c == FilterColor.Green) ? (greenOn ?? greenOff) : greenOff;
            if (blueRing != null)  blueRing.sprite  = (c == FilterColor.Blue)  ? (blueOn  ?? blueOff)  : blueOff;

            // Legacy dot mode (runs in parallel if still wired)
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
