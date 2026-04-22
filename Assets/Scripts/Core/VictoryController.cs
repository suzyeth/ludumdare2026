using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PrismZone.Core
{
    /// <summary>
    /// Parallel to <see cref="GameOverController"/>. Fires when the player reaches
    /// an exit trigger that has no next-scene configured (final escape). UI listens
    /// to <see cref="OnVictory"/> to reveal its panel.
    /// </summary>
    public static class VictoryController
    {
        public static event Action<string> OnVictory;
        /// <summary>Fired right before scene reload so persistent UI (VictoryPanel) can reset alpha.</summary>
        public static event Action OnReset;
        public static bool IsVictory { get; private set; }

        public static void TriggerVictory(string reason = "escaped")
        {
            if (IsVictory) return;
            if (GameOverController.IsGameOver) return;
            IsVictory = true;
            Time.timeScale = 0f;
            OnVictory?.Invoke(reason);
            Debug.Log($"[Victory] Triggered: {reason}");
        }

        public static void Restart()
        {
            // Restart = back to main menu. Same rationale as GameOverController.Restart.
            GotoMainMenu();
        }

        public static void GotoMainMenu(string menuScene = "Scene_MainMenu")
        {
            ResetState();
            TeardownPersistent();
            SceneManager.LoadSceneAsync(menuScene);
        }

        private static void TeardownPersistent()
        {
            var bundle = GameObject.Find("_Persistent");
            if (bundle != null) UnityEngine.Object.Destroy(bundle);
        }

        /// <summary>Clear transient state without reloading. Safe from MainMenu.Awake.</summary>
        public static void ResetState()
        {
            Time.timeScale = 1f;
            IsVictory = false;
            SceneTransition.SetPendingSpawn(null);
            OnReset?.Invoke();
        }
    }
}
