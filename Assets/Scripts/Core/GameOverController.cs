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
            // Restart = back to main menu so the player can choose to retry or quit.
            // Destroys the persistent bundle first; menu renders standalone.
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
            // DontDestroyOnLoad keeps _Persistent alive across LoadScene. Kill it so
            // MainMenu starts clean (no leftover HUD, Player, Camera, AudioListener).
            var bundle = GameObject.Find("_Persistent");
            if (bundle != null) UnityEngine.Object.Destroy(bundle);
        }

        /// <summary>
        /// Reset transient state (timeScale / flags / UI) WITHOUT reloading the scene.
        /// Use from freshly-loaded scenes (MainMenu.Awake etc.) so you don't recurse
        /// through LoadScene → Awake → LoadScene …
        /// </summary>
        public static void ResetState()
        {
            Time.timeScale = 1f;
            IsGameOver = false;
            OnReset?.Invoke();
        }
    }
}
