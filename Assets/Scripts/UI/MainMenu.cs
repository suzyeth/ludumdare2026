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
        [SerializeField] private Button quitButton;

        private void Awake()
        {
            // MainMenu is entered fresh — clear any GameOver state from prior run.
            GameOverController.Restart();
            Time.timeScale = 1f;

            if (titleLabel != null) titleLabel.text = I18nManager.Get("ui.menu.title");
            if (startButton != null) startButton.onClick.AddListener(StartGame);
            if (quitButton != null) quitButton.onClick.AddListener(Quit);
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
