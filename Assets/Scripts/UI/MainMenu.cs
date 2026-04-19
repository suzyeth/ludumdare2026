using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using PrismZone.Core;

namespace PrismZone.UI
{
    /// <summary>
    /// Main menu wired to Start / Quit / Language-switch buttons. Start loads the
    /// first gameplay scene (defaults to "SampleScene"; designers edit the field).
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private string firstScene = "SampleScene";
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private Button startButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        private void Awake()
        {
            // MainMenu is entered fresh — clear prior-run transient state.
            // MUST use ResetState (not Restart) or we'd recursively reload this scene.
            GameOverController.ResetState();
            VictoryController.ResetState();  // parity: prior-run Victory also cleared
            Time.timeScale = 1f;

            if (titleLabel != null) titleLabel.text = I18nManager.Get("ui.menu.title");
            if (startButton != null) startButton.onClick.AddListener(StartGame);
            if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
            if (quitButton != null) quitButton.onClick.AddListener(Quit);
        }

        public void OpenSettings()
        {
            if (SettingsPanel.Instance == null) return;
            // Hide menu content so it doesn't bleed through the Settings backdrop.
            // We toggle specific children — not a CanvasGroup on self — because the
            // MainMenu component sits on the Canvas root, which also hosts the
            // SettingsPanel child. Hiding the whole Canvas would hide Settings too.
            SetMenuVisible(false);
            SettingsPanel.Instance.OnClosed -= OnSettingsClosed;
            SettingsPanel.Instance.OnClosed += OnSettingsClosed;
            SettingsPanel.Instance.Open();
        }

        private void OnSettingsClosed()
        {
            if (SettingsPanel.Instance != null)
                SettingsPanel.Instance.OnClosed -= OnSettingsClosed;
            SetMenuVisible(true);
        }

        private void SetMenuVisible(bool on)
        {
            if (titleLabel != null) titleLabel.gameObject.SetActive(on);
            if (startButton != null) startButton.gameObject.SetActive(on);
            if (settingsButton != null) settingsButton.gameObject.SetActive(on);
            if (quitButton != null) quitButton.gameObject.SetActive(on);
        }

        private void StartGame()
        {
            SceneManager.LoadSceneAsync(firstScene);
        }

        private void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
