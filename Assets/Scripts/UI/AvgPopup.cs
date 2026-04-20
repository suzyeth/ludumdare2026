using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PrismZone.UI
{
    /// <summary>
    /// One popup slot in the HUD that can render any of the 5 AVG styles. Designer
    /// wires one instance per style into <see cref="DialogueManager"/>; the manager
    /// picks which to activate based on <see cref="DialogueType"/>.
    ///
    /// Behaviour depends on configured flags:
    ///   - <see cref="skippable"/> (default true): Esc / click finishes the typewriter;
    ///     a second press closes the popup.
    ///   - <see cref="autoDismissSeconds"/> (>0): closes itself after N real-time seconds.
    ///     Useful for TIP (4–5 s). Set 0 to require a manual close.
    ///
    /// Multi-page READ: <see cref="Show(string[], Action)"/> shows one key per page,
    /// advancing on primary press.
    /// </summary>
    public class AvgPopup : MonoBehaviour
    {
        [SerializeField] private DialogueType type;
        [SerializeField] private TMP_Text bodyLabel;
        [SerializeField] private TMP_Text pageCounterLabel;   // optional, READ only
        [SerializeField] private TMP_Text titleLabel;         // optional, READ only — set via SetHeader
        [SerializeField] private Image    headerImage;        // optional, READ only — set via SetHeader
        [SerializeField] private TypewriterText typewriter;
        [SerializeField] private bool skippable = true;
        [SerializeField] private float autoDismissSeconds = 0f;
        [Tooltip("FLASH only: minimum seconds the popup must be on-screen before primary input can advance it. Auto-advances after this when typewriter is finished.")]
        [SerializeField] private float minDisplaySeconds = 0f;

        [Header("Page Nav (optional, READ)")]
        [Tooltip("Button hidden on first page and single-page popups; advances to the next page when clicked.")]
        [SerializeField] private UnityEngine.UI.Button nextPageButton;
        [Tooltip("Button hidden on first page and single-page popups; goes back one page when clicked.")]
        [SerializeField] private UnityEngine.UI.Button prevPageButton;

        private CanvasGroup _group;
        private string[] _pages;
        private int _pageIdx;
        private Action _onFinished;
        private float _autoDismissAt = -1f;
        private float _shownAt;
        private bool  _autoAdvancePrimed;

        public DialogueType Type => type;
        public bool IsOpen => _group != null && _group.alpha > 0.5f;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            SetVisible(false);
            if (typewriter == null) typewriter = GetComponentInChildren<TypewriterText>();
            if (nextPageButton != null) nextPageButton.onClick.AddListener(NextPage);
            if (prevPageButton != null) prevPageButton.onClick.AddListener(PrevPage);
        }

        /// <summary>Button-driven advance. Skips the typewriter on the current page if still rolling; otherwise moves forward one page (or closes on the last page).</summary>
        public void NextPage()
        {
            if (!IsOpen) return;
            if (typewriter != null && typewriter.IsRolling)
            {
                if (skippable) typewriter.CompleteImmediately();
                return;
            }
            if (_pages != null && _pageIdx < _pages.Length - 1)
            {
                _pageIdx++;
                RenderCurrentPage();
            }
            else
            {
                Close();
            }
        }

        /// <summary>Button-driven retreat. No-op on the first page. Does not re-open closed popups.</summary>
        public void PrevPage()
        {
            if (!IsOpen) return;
            if (_pages == null || _pageIdx <= 0) return;
            _pageIdx--;
            RenderCurrentPage();
        }

        /// <summary>Single-page show. Text is the resolved (not i18n) string.</summary>
        public void Show(string fullText, Action onFinished = null)
        {
            Show(new[] { fullText ?? string.Empty }, onFinished);
        }

        /// <summary>Multi-page show. READ advances through pages; others use page 0 only.</summary>
        public void Show(string[] pages, Action onFinished = null)
        {
            if (pages == null || pages.Length == 0) pages = new[] { string.Empty };
            _pages = pages;
            _pageIdx = 0;
            _onFinished = onFinished;
            _shownAt = Time.unscaledTime;
            _autoAdvancePrimed = false;
            SetVisible(true);
            RenderCurrentPage();
            if (autoDismissSeconds > 0f) _autoDismissAt = Time.unscaledTime + autoDismissSeconds;
        }

        /// <summary>READ helper: designer hands per-node title + sprite to the popup.</summary>
        public void SetHeader(string title, Sprite sprite)
        {
            if (titleLabel != null) titleLabel.text = title ?? string.Empty;
            if (headerImage != null)
            {
                if (sprite != null) { headerImage.sprite = sprite; headerImage.enabled = true; }
                else headerImage.enabled = false;
            }
        }

        /// <summary>
        /// Manager ticks this each frame so popups respond uniformly. Returns true if
        /// the popup has finished (should be cleared from the manager's active slot).
        /// </summary>
        public bool Tick(bool primaryPressed, bool cancelPressed)
        {
            if (!IsOpen) return true;

            if (autoDismissSeconds > 0f && Time.unscaledTime >= _autoDismissAt)
            {
                Close();
                return true;
            }

            // FLASH minDisplaySeconds: after the typewriter finishes AND min display is up,
            // auto-advance once (no user input needed). Prevents the player getting stuck on
            // an unskippable line, while still respecting the gravitas hold time.
            if (minDisplaySeconds > 0f
                && (typewriter == null || !typewriter.IsRolling)
                && Time.unscaledTime - _shownAt >= minDisplaySeconds
                && !_autoAdvancePrimed)
            {
                _autoAdvancePrimed = true;
                primaryPressed = true; // synthesise a press so the existing branch handles close/advance
            }

            if (primaryPressed || cancelPressed)
            {
                // Typewriter still rolling → jump to full text.
                if (typewriter != null && typewriter.IsRolling)
                {
                    if (skippable) typewriter.CompleteImmediately();
                    return false;
                }

                if (cancelPressed && !skippable) return false;

                // Advance to next page if any remain; otherwise close. Works for
                // any popup type whose TSV cell has '|' page breaks (NAR/READ/TIP).
                if (_pages != null && _pageIdx < _pages.Length - 1)
                {
                    _pageIdx++;
                    RenderCurrentPage();
                    return false;
                }
                Close();
                return true;
            }
            return false;
        }

        public void Close()
        {
            if (!IsOpen) return;
            SetVisible(false);
            var cb = _onFinished;
            _onFinished = null;
            cb?.Invoke();
        }

        private void RenderCurrentPage()
        {
            if (bodyLabel == null) return;
            string text = _pages != null && _pageIdx < _pages.Length ? _pages[_pageIdx] : string.Empty;
            if (typewriter != null) typewriter.Play(text);
            else bodyLabel.text = text;

            bool multiPage = _pages != null && _pages.Length > 1;
            if (pageCounterLabel != null)
                pageCounterLabel.text = multiPage ? $"{_pageIdx + 1}/{_pages.Length}" : string.Empty;

            // Hide buttons on single-page popups entirely; on multi-page, hide the
            // nav arrow that has nothing to go to (no "previous" on page 0, no
            // "next" past the last page) — the primary input and close button
            // still work, so this is purely a visual hint.
            if (prevPageButton != null)
                prevPageButton.gameObject.SetActive(multiPage && _pageIdx > 0);
            if (nextPageButton != null)
                nextPageButton.gameObject.SetActive(multiPage && _pageIdx < _pages.Length - 1);
        }

        private void SetVisible(bool on)
        {
            if (_group == null) return;
            _group.alpha = on ? 1f : 0f;
            _group.interactable = on;
            _group.blocksRaycasts = on;
        }

        // World-space anchoring for ENV bubbles.
        public void AnchorToWorld(Vector3 worldPos, Camera cam)
        {
            if (type != DialogueType.ENV || cam == null) return;
            var rt = (RectTransform)transform;
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            Vector2 screen = cam.WorldToScreenPoint(worldPos);
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                rt.position = screen;
            }
            else
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    (RectTransform)canvas.transform, screen, canvas.worldCamera, out var lp);
                rt.localPosition = lp;
            }
        }
    }
}
