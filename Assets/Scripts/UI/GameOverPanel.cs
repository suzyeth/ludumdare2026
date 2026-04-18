using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PrismZone.Core;

namespace PrismZone.UI
{
    /// <summary>
    /// Full-screen "GAME OVER" panel. Subscribes to GameOverController.OnGameOver,
    /// shows itself via CanvasGroup, and its Restart button calls GameOverController.Restart().
    /// </summary>
    public class GameOverPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text reasonLabel;
        [SerializeField] private Button restartButton;

        private CanvasGroup _group;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            SetVisible(false);
            if (restartButton != null) restartButton.onClick.AddListener(() => GameOverController.Restart());
        }

        private void OnEnable()
        {
            GameOverController.OnGameOver += Show;
            GameOverController.OnReset += HandleReset;
        }

        private void OnDisable()
        {
            GameOverController.OnGameOver -= Show;
            GameOverController.OnReset -= HandleReset;
        }

        private void HandleReset() => SetVisible(false);

        private void Show(string reason)
        {
            if (titleLabel != null) titleLabel.text = I18nManager.Get("ui.gameover.title");
            if (reasonLabel != null) reasonLabel.text = I18nManager.Get("ui.gameover.reason." + reason);
            SetVisible(true);
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
