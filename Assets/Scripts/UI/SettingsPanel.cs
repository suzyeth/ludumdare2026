using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using PrismZone.Core;

namespace PrismZone.UI
{
    /// <summary>
    /// Reusable settings panel. Reads/writes <see cref="GameSettings"/>. Shown from
    /// MainMenu (via <see cref="MainMenu.OpenSettings"/>) and from PauseMenu
    /// (<see cref="PauseMenu.OpenSettings"/>). Because it lives on HUD_Canvas
    /// (persistent), the same instance serves in-game — no separate prefab per scene.
    ///
    /// Esc closes the panel. While open, Pause/Main-menu interaction is blocked
    /// by CanvasGroup blocksRaycasts.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        public static SettingsPanel Instance { get; private set; }

        [Header("Labels (keys resolved via I18nManager)")]
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text masterLabel;
        [SerializeField] private TMP_Text sfxLabel;
        [SerializeField] private TMP_Text musicLabel;
        [SerializeField] private TMP_Text languageLabel;

        [Header("Controls")]
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private TMP_Text masterValueLabel;
        [SerializeField] private TMP_Text sfxValueLabel;
        [SerializeField] private TMP_Text musicValueLabel;
        [SerializeField] private Button languageButton;
        [SerializeField] private TMP_Text languageButtonLabel;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button closeButton;

        [Tooltip("Supported language codes, cycled by the language button.")]
        [SerializeField] private string[] languages = new[] { "en", "zh" };

        private CanvasGroup _group;
        private bool _suppressCallbacks;

        public bool IsOpen => _group != null && _group.alpha > 0.5f;

        /// <summary>
        /// Fires after Close() finishes. MainMenu / PauseMenu subscribe so they can
        /// restore their own hidden buttons — see <see cref="MainMenu.OpenSettings"/>.
        /// </summary>
        public event System.Action OnClosed;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            SetVisible(false);

            if (masterSlider   != null) { masterSlider.minValue = 0f;   masterSlider.maxValue = 1f; masterSlider.onValueChanged.AddListener(OnMaster); }
            if (sfxSlider      != null) { sfxSlider.minValue = 0f;      sfxSlider.maxValue = 1f;    sfxSlider.onValueChanged.AddListener(OnSfx); }
            if (musicSlider    != null) { musicSlider.minValue = 0f;    musicSlider.maxValue = 1f;  musicSlider.onValueChanged.AddListener(OnMusic); }
            if (languageButton != null) languageButton.onClick.AddListener(CycleLanguage);
            if (resetButton    != null) resetButton.onClick.AddListener(GameSettings.ResetToDefaults);
            if (closeButton    != null) closeButton.onClick.AddListener(Close);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            GameSettings.OnChanged -= SyncFromModel;
            GameSettings.OnChanged += SyncFromModel;
        }

        private void OnDisable()
        {
            GameSettings.OnChanged -= SyncFromModel;
        }

        // Esc closes the panel — pause-menu Esc-to-close happens via PauseMenu
        // priority chain (which checks IsOpen first).
        private void Update()
        {
            if (!IsOpen) return;
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) Close();
        }

        public void Open()
        {
            ApplyLabels();
            SyncFromModel();
            SetVisible(true);
        }

        public void Close()
        {
            if (!IsOpen) return;
            SetVisible(false);
            OnClosed?.Invoke();
        }

        private void OnMaster(float v) { if (_suppressCallbacks) return; GameSettings.MasterVolume = v; }
        private void OnSfx(float v)    { if (_suppressCallbacks) return; GameSettings.SfxVolume = v; }
        private void OnMusic(float v)  { if (_suppressCallbacks) return; GameSettings.MusicVolume = v; }

        private void CycleLanguage()
        {
            if (languages == null || languages.Length == 0) return;
            var cur = GameSettings.Language;
            int idx = System.Array.IndexOf(languages, cur);
            idx = (idx + 1) % languages.Length;
            var next = languages[idx];
            GameSettings.Language = next;
            // Apply immediately so every LocalizedText on screen re-resolves via
            // I18nManager.OnLanguageChanged.
            I18nManager.SetLanguage(next);
            ApplyLabels();
        }

        private void ApplyLabels()
        {
            if (titleLabel     != null) titleLabel.text     = I18nManager.Get("ui.settings.title");
            if (masterLabel    != null) masterLabel.text    = I18nManager.Get("ui.settings.master");
            if (sfxLabel       != null) sfxLabel.text       = I18nManager.Get("ui.settings.sfx");
            if (musicLabel     != null) musicLabel.text     = I18nManager.Get("ui.settings.music");
            if (languageLabel  != null) languageLabel.text  = I18nManager.Get("ui.settings.language");
            if (languageButtonLabel != null)
                languageButtonLabel.text = I18nManager.Get("ui.settings.lang." + GameSettings.Language);
        }

        private void SyncFromModel()
        {
            _suppressCallbacks = true;
            try
            {
                if (masterSlider != null) masterSlider.value = GameSettings.MasterVolume;
                if (sfxSlider    != null) sfxSlider.value    = GameSettings.SfxVolume;
                if (musicSlider  != null) musicSlider.value  = GameSettings.MusicVolume;
            }
            finally { _suppressCallbacks = false; }

            if (masterValueLabel != null) masterValueLabel.text = FormatPct(GameSettings.MasterVolume);
            if (sfxValueLabel    != null) sfxValueLabel.text    = FormatPct(GameSettings.SfxVolume);
            if (musicValueLabel  != null) musicValueLabel.text  = FormatPct(GameSettings.MusicVolume);
            if (languageButtonLabel != null)
                languageButtonLabel.text = I18nManager.Get("ui.settings.lang." + GameSettings.Language);
        }

        private static string FormatPct(float v) => Mathf.RoundToInt(v * 100f) + "%";

        private void SetVisible(bool on)
        {
            if (_group == null) return;
            _group.alpha = on ? 1f : 0f;
            _group.interactable = on;
            _group.blocksRaycasts = on;
        }
    }
}
