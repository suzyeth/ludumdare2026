using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PrismZone.Core
{
    /// <summary>
    /// Global fail-state hub. Anything that wants to end the run calls
    /// <see cref="TriggerGameOver"/>. UI listens to <see cref="OnGameOver"/> to
    /// reveal its panel, and calls <see cref="Restart"/> when the player clicks
    /// the button. Simple scene-reload for now; scene-transition systems can
    /// hook the same events later.
    /// </summary>
    public static class GameOverController
    {
        public static event Action<string> OnGameOver;
        /// <summary>Fired right before the scene reloads so persistent UIs can reset alpha/state.</summary>
        public static event Action OnReset;
        public static bool IsGameOver { get; private set; }

        public static void TriggerGameOver(string reason = "caught")
        {
            if (IsGameOver) return;
            IsGameOver = true;
            Time.timeScale = 0f;   // freeze gameplay, UI still draws
            OnGameOver?.Invoke(reason);
            Debug.Log($"[GameOver] Triggered: {reason}");
        }

        public static void Restart()
        {
            // Reset runtime state BEFORE the scene reload so persistent UI
            // (GameOverPanel on HUD_Canvas) hides in the same frame.
            Time.timeScale = 1f;
            IsGameOver = false;
            OnReset?.Invoke();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
