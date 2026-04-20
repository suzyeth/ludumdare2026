using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using PrismZone.Core;

namespace PrismZone.UI
{
    /// <summary>
    /// Center-screen clue / dialog popup. Single instance. Show(key) opens it with
    /// the i18n text; E or Space dismisses. Not a pause — enemies keep ticking.
    /// </summary>
    public class CluePopup : MonoBehaviour
    {
        public static CluePopup Instance { get; private set; }

        [SerializeField] private TMP_Text body;

        private CanvasGroup _group;

        public bool IsOpen => _group != null && _group.alpha > 0.5f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.interactable = false;
            _group.blocksRaycasts = false;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (!IsOpen) return;
            var kb = Keyboard.current;
            if (kb == null) return;
            // Esc + E both dismiss — matches ItemDetailPanel/PasscodePanel/SettingsPanel.
            // PauseMenu has DefaultExecutionOrder(-10) so its Esc priority chain runs
            // first and closes us via Close(); our own IsOpen early-return above then
            // prevents PauseMenu falling through to toggle Pause when we run later.
            if (kb.eKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame) Close();
        }

        public void Show(string i18nKey)
        {
            if (body != null) body.text = I18nManager.Get(i18nKey);
            if (_group != null) { _group.alpha = 1f; }
            AudioManager.Instance?.Play(SoundId.PopupOpen);
        }

        public void Close()
        {
            bool wasOpen = IsOpen;
            if (_group != null) { _group.alpha = 0f; }
            if (wasOpen) AudioManager.Instance?.Play(SoundId.PopupClose);
        }
    }
}
