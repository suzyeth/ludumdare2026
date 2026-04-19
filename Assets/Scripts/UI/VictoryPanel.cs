using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PrismZone.Core;

namespace PrismZone.UI
{
    /// <summary>
    /// Full-screen victory panel. Shows "ESCAPED" + Restart / Main Menu.
    /// Subscribes to VictoryController.OnVictory + OnReset (hides on scene reload).
    /// </summary>
    public class VictoryPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text reasonLabel;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;

        private CanvasGroup _group;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            SetVisible(false);
            if (restartButton != null) restartButton.onClick.AddListener(VictoryController.Restart);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(() => VictoryController.GotoMainMenu());
        }

        private void OnEnable()
        {
            // Idempotent re-subscribe — protects against dup subscriptions if the
            // panel lives on a persistent HUD and OnEnable fires more than once.
            VictoryController.OnVictory -= Show;
            VictoryController.OnReset -= HandleReset;
            VictoryController.OnVictory += Show;
            VictoryController.OnReset += HandleReset;
        }

        private void OnDisable()
        {
            VictoryController.OnVictory -= Show;
            VictoryController.OnReset -= HandleReset;
        }

        private void HandleReset() => SetVisible(false);

        private void Show(string reason)
        {
            if (titleLabel != null) titleLabel.text = I18nManager.Get("ui.victory.title");
            if (reasonLabel != null) reasonLabel.text = I18nManager.Get("ui.victory.reason." + reason);
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
