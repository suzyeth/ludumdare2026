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
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        public static PauseMenu Instance { get; private set; }

        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private string mainMenuScene = "Scene_MainMenu";

        private CanvasGroup _group;

        public bool IsOpen => _group != null && _group.alpha > 0.5f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            SetVisible(false);

            if (resumeButton   != null) resumeButton.onClick.AddListener(Resume);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(GotoMainMenu);
            if (quitButton     != null) quitButton.onClick.AddListener(Quit);
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (!kb.escapeKey.wasPressedThisFrame) return;
            HandleEscape();
        }

        private void HandleEscape()
        {
            // Never open pause over GameOver
            if (GameOverController.IsGameOver) return;

            // 1) close transient modals before opening pause
            if (CluePopup.Instance != null && CluePopup.Instance.IsOpen)
            { CluePopup.Instance.Close(); return; }
            if (ItemDetailPanel.Instance != null && ItemDetailPanel.Instance.IsOpen)
            { ItemDetailPanel.Instance.Close(); return; }
            var passcode = FindFirstObjectByType<PasscodePanel>();
            if (passcode != null && passcode.IsOpen) { passcode.Close(); return; }

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
#else
            Application.Quit();
#endif
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
