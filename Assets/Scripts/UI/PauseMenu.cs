using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using PrismZone.Core;

namespace PrismZone.UI
{
    /// <summary>
    /// Esc-toggled pause menu. Freezes Time.timeScale = 0 while open.
    /// Priority order when Esc pressed:
    ///   1. If CluePopup open → close it first
    ///   2. If ItemDetailPanel open → close it first
    ///   3. If PasscodePanel open → close it first
    ///   4. Otherwise toggle PauseMenu
    ///
    /// Runs before default-order modals (CluePopup, ItemDetailPanel, PasscodePanel,
    /// SettingsPanel) so the priority chain consumes Esc first; otherwise those
    /// modals' own Esc handlers could close themselves and leave PauseMenu to
    /// fall through to toggle Pause on the same frame.
    /// </summary>
    [DefaultExecutionOrder(-10)]
    public class PauseMenu : MonoBehaviour
    {
        public static PauseMenu Instance { get; private set; }

        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button musicToggleButton;
        [SerializeField] private TMP_Text musicToggleLabel;
        [SerializeField] private string mainMenuScene = "Scene_MainMenu";

        // Remember the user's music level before muting so toggling On restores it
        // instead of restoring the default.
        private const string PK_MusicLastNonZero = "pz.music.lastNonZero";

        private CanvasGroup _group;
        private PasscodePanel _cachedPasscode;

        public bool IsOpen => _group != null && _group.alpha > 0.5f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            SetVisible(false);

            if (resumeButton       != null) resumeButton.onClick.AddListener(Resume);
            if (settingsButton     != null) settingsButton.onClick.AddListener(OpenSettings);
            if (mainMenuButton     != null) mainMenuButton.onClick.AddListener(GotoMainMenu);
            if (quitButton         != null) quitButton.onClick.AddListener(QuitToMenu);
            if (musicToggleButton  != null) musicToggleButton.onClick.AddListener(ToggleMusic);
            RefreshMusicLabel();
        }

        public void ToggleMusic()
        {
            float cur = GameSettings.MusicVolume;
            if (cur > 0f)
            {
                PlayerPrefs.SetFloat(PK_MusicLastNonZero, cur);
                GameSettings.MusicVolume = 0f;
            }
            else
            {
                float restore = PlayerPrefs.GetFloat(PK_MusicLastNonZero, GameSettings.DefaultMusic);
                if (restore <= 0f) restore = GameSettings.DefaultMusic;
                GameSettings.MusicVolume = restore;
            }
            RefreshMusicLabel();
        }

        private void RefreshMusicLabel()
        {
            if (musicToggleLabel == null) return;
            musicToggleLabel.text = I18nManager.Get(GameSettings.MusicVolume > 0f
                ? "ui.pause.music_on" : "ui.pause.music_off");
        }

        public void OpenSettings()
        {
            if (SettingsPanel.Instance == null) return;
            // PauseMenu is a sibling of SettingsPanel, so hiding its CanvasGroup
            // is safe (does not hide Settings). Time.timeScale stays 0 throughout.
            SetVisible(false);
            SettingsPanel.Instance.OnClosed -= OnSettingsClosed;
            SettingsPanel.Instance.OnClosed += OnSettingsClosed;
            SettingsPanel.Instance.Open();
        }

        private void OnSettingsClosed()
        {
            if (SettingsPanel.Instance != null)
                SettingsPanel.Instance.OnClosed -= OnSettingsClosed;
            // Only reopen the pause backdrop if we're still in a paused state
            // (user didn't click Main Menu / Quit from within Settings).
            if (Time.timeScale == 0f) SetVisible(true);
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            // Esc toggles pause as before.
            if (kb.escapeKey.wasPressedThisFrame) { HandleEscape(); return; }
            // Keyboard fallbacks — Unity's new Input System sometimes drops
            // Button click dispatch at Time.timeScale = 0. Map each pause
            // action to a dedicated key so players can always control the menu.
            if (IsOpen)
            {
                if (kb.spaceKey.wasPressedThisFrame
                    || kb.enterKey.wasPressedThisFrame
                    || kb.numpadEnterKey.wasPressedThisFrame
                    || kb.rKey.wasPressedThisFrame)                // R = Resume
                {
                    Resume();
                    return;
                }
                if (kb.mKey.wasPressedThisFrame)                    // M = Music toggle
                {
                    ToggleMusic();
                    return;
                }
                if (kb.sKey.wasPressedThisFrame)                    // S = Settings
                {
                    OpenSettings();
                    return;
                }
                if (kb.hKey.wasPressedThisFrame)                    // H = Home (main menu)
                {
                    GotoMainMenu();
                    return;
                }
                // Debug: confirm pointer clicks reach the buttons. Any click
                // fires the standard onClick; if we ALSO see nothing here, the
                // issue is further upstream (canvas raycaster / blocker).
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                    Debug.Log("[PauseMenu] mouse click detected at timeScale=" + Time.timeScale);
            }
        }

        private void HandleEscape()
        {
            // Never open pause over GameOver
            if (GameOverController.IsGameOver) return;

            // 1) close transient modals before opening pause
            // Settings panel takes priority: it can be opened from Pause or MainMenu,
            // so its Esc must close it first without toggling Pause below it.
            if (SettingsPanel.Instance != null && SettingsPanel.Instance.IsOpen)
            { SettingsPanel.Instance.Close(); return; }
            if (CluePopup.Instance != null && CluePopup.Instance.IsOpen)
            { CluePopup.Instance.Close(); return; }
            if (ItemDetailPanel.Instance != null && ItemDetailPanel.Instance.IsOpen)
            { ItemDetailPanel.Instance.Close(); return; }
            if (_cachedPasscode == null)
                _cachedPasscode = FindFirstObjectByType<PasscodePanel>();
            if (_cachedPasscode != null && _cachedPasscode.IsOpen)
            { _cachedPasscode.Close(); return; }

            // 2) toggle pause
            if (IsOpen) Resume();
            else Pause();
        }

        public void Pause()
        {
            if (titleLabel != null) titleLabel.text = I18nManager.Get("ui.pause.title");
            SetVisible(true);
            Time.timeScale = 0f;
        }

        public void Resume()
        {
            SetVisible(false);
            Time.timeScale = 1f;
        }

        public void GotoMainMenu()
        {
            Time.timeScale = 1f;
            SetVisible(false);
            SceneManager.LoadSceneAsync(mainMenuScene);
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
            // WebGL has no real Application.Quit — fall back to dropping the player
            // back at the main menu so the button still does something visible.
            GotoMainMenu();
#else
            Application.Quit();
#endif
        }

        /// <summary>Alias used by inspector wiring on Quit-to-Menu button (drops timescale + loads menu).</summary>
        public void QuitToMenu() => GotoMainMenu();

        private void SetVisible(bool on)
        {
            if (_group == null) return;
            _group.alpha = on ? 1f : 0f;
            _group.interactable = on;
            _group.blocksRaycasts = on;
        }
    }
}
