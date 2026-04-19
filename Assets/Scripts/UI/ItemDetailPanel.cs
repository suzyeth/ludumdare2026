using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using PrismZone.Core;

namespace PrismZone.UI
{
    /// <summary>
    /// Center-screen "big image + multi-page text" viewer. Open via Show(ItemData).
    /// F or Esc to close, ←/→ to flip pages.
    /// Driven by a CanvasGroup for alpha-based show/hide (same pattern as CluePopup).
    /// </summary>
    public class ItemDetailPanel : MonoBehaviour
    {
        public static ItemDetailPanel Instance { get; private set; }

        [SerializeField] private Image bigImage;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text bodyLabel;
        [SerializeField] private TMP_Text pageCountLabel;

        private CanvasGroup _group;
        private ItemData _current;
        private int _page;
        // Guard: F opens this panel via PlayerInteraction and also closes it via
        // our own Update. Both run on the same frame of the F press, so without
        // this frame-stamp the panel opens and immediately re-closes.
        private int _openedFrame = -1;

        public bool IsOpen => _group != null && _group.alpha > 0.5f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            SetVisible(false);
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Update()
        {
            if (!IsOpen) return;
            if (Time.frameCount == _openedFrame) return; // swallow same-frame F press that opened us
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.escapeKey.wasPressedThisFrame) { Close(); return; }
            if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) FlipPage(-1);
            if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) FlipPage(+1);
        }

        public void Show(ItemData item)
        {
            if (item == null || !item.HasDetailPopup) return;
            _current = item;
            _page = 0;
            Refresh();
            SetVisible(true);
            _openedFrame = Time.frameCount;
        }

        public void Close() { SetVisible(false); _current = null; }

        private void FlipPage(int delta)
        {
            if (_current == null) return;
            int max = Mathf.Max(1, _current.PageCount);
            _page = Mathf.Clamp(_page + delta, 0, max - 1);
            Refresh();
        }

        private void Refresh()
        {
            if (_current == null) return;
            if (bigImage != null) { bigImage.sprite = _current.BigIcon; bigImage.enabled = _current.BigIcon != null; }
            if (nameLabel != null) nameLabel.text = I18nManager.Get(_current.NameKey);
            if (bodyLabel != null)
            {
                // Defensive: PageKeys can be null (SO field never wired) or length < _page+1.
                string bodyKey = "";
                var keys = _current.PageKeys;
                if (keys != null && _page >= 0 && _page < keys.Length) bodyKey = keys[_page];
                bodyLabel.text = I18nManager.Get(bodyKey);
            }
            if (pageCountLabel != null)
            {
                pageCountLabel.text = _current.PageCount > 1
                    ? $"{_page + 1}/{_current.PageCount}"
                    : "";
            }
        }

        private void SetVisible(bool on)
        {
            if (_group == null) return;
            _group.alpha = on ? 1f : 0f;
            _group.interactable = on;
            _group.blocksRaycasts = on;
        }
    }
}
