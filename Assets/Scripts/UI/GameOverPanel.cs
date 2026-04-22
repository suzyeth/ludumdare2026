using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
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
            // Idempotent: remove first in case OnEnable fires more than once on a
            // persistent HUD (subscription leak protection).
            GameOverController.OnGameOver -= Show;
            GameOverController.OnReset -= HandleReset;
            GameOverController.OnGameOver += Show;
            GameOverController.OnReset += HandleReset;
        }

        private void OnDisable()
        {
            GameOverController.OnGameOver -= Show;
            GameOverController.OnReset -= HandleReset;
        }

        private void HandleReset() => SetVisible(false);

        private float _shownAt;

        private void Show(string reason)
        {
            if (titleLabel != null) titleLabel.text = I18nManager.Get("ui.gameover.title");
            if (reasonLabel != null) reasonLabel.text = I18nManager.Get("ui.gameover.reason." + reason);
            SetVisible(true);
            _shownAt = Time.unscaledTime;
        }

        private void Update()
        {
            // Keyboard / mouse / gamepad fallback — some click-event paths die at
            // timeScale=0 or when a stale blocker canvas is in the scene. Any
            // input short-circuits straight to Restart. 0.4s input lock lets the
            // "caught" animation settle so the player doesn't reflexively skip.
            if (_group == null || _group.alpha < 0.5f) return;
            if (Time.unscaledTime - _shownAt < 0.4f) return;
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            // Accept only deliberate confirm-style inputs so a player still
            // holding WASD through the catch animation doesn't reflex-skip.
            bool pressed = mouse != null && mouse.leftButton.wasPressedThisFrame;
            if (!pressed && kb != null)
            {
                pressed = kb.spaceKey.wasPressedThisFrame
                       || kb.enterKey.wasPressedThisFrame
                       || kb.numpadEnterKey.wasPressedThisFrame
                       || kb.rKey.wasPressedThisFrame;
#if !UNITY_WEBGL
                // On WebGL, Esc is consumed by the browser to release pointer-lock.
                // Mapping it to Restart here causes an immediate restart the moment
                // the player exits pointer-lock, which is never intentional.
                if (!pressed) pressed = kb.escapeKey.wasPressedThisFrame;
#endif
            }
            if (pressed) GameOverController.Restart();
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
