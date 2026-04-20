using UnityEngine;
using UnityEngine.InputSystem;
using PrismZone.Core;
using PrismZone.UI;

namespace PrismZone.DebugTools
{
    /// <summary>
    /// Dev-only quick cycler for every UI surface in the game.
    ///
    ///   1  — NAR popup   (T-01)
    ///   2  — READ popup  (item.diary.page.1)
    ///   3  — FLASH popup (T-17)
    ///   4  — CluePopup   (clue.room1.code)
    ///   5  — PauseMenu   (toggle)
    ///   6  — SettingsPanel
    ///   7  — Game Over
    ///   8  — Victory
    ///   9  — Flashback montage (PlayFrames)
    ///   0  — Reset all (close popups, clear game-over/victory, resume time)
    ///
    /// Drop on any scene-root GameObject while testing. Remove before release.
    /// </summary>
    public class UIPanelTester : MonoBehaviour
    {
        [SerializeField] private string narNode   = "T-01";
        [SerializeField] private string readNode  = "item.diary.page.1";
        [SerializeField] private string flashNode = "T-17";
        [SerializeField] private string clueKey   = "clue.room1.code";

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.digit1Key.wasPressedThisFrame) FireNar();
            else if (kb.digit2Key.wasPressedThisFrame) FireRead();
            else if (kb.digit3Key.wasPressedThisFrame) FireFlash();
            else if (kb.digit4Key.wasPressedThisFrame) FireClue();
            else if (kb.digit5Key.wasPressedThisFrame) TogglePause();
            else if (kb.digit6Key.wasPressedThisFrame) OpenSettings();
            else if (kb.digit7Key.wasPressedThisFrame) FireGameOver();
            else if (kb.digit8Key.wasPressedThisFrame) FireVictory();
            else if (kb.digit9Key.wasPressedThisFrame) FireFlashback();
            else if (kb.digit0Key.wasPressedThisFrame) ResetAll();
        }

        private void FireNar()
        {
            if (DialogueManager.Instance == null) { Debug.LogWarning("[UITester] no DialogueManager"); return; }
            DialogueManager.Instance.ShowById(narNode);
        }

        private void FireRead()
        {
            if (DialogueManager.Instance == null) return;
            DialogueManager.Instance.ShowById(readNode, null, null, "item.diary", null);
        }

        private void FireFlash()
        {
            if (DialogueManager.Instance == null) return;
            DialogueManager.Instance.ShowById(flashNode);
        }

        private void FireClue()
        {
            if (CluePopup.Instance == null) { Debug.LogWarning("[UITester] no CluePopup"); return; }
            CluePopup.Instance.Show(clueKey);
        }

        private void TogglePause()
        {
            if (PauseMenu.Instance == null) { Debug.LogWarning("[UITester] no PauseMenu"); return; }
            if (PauseMenu.Instance.IsOpen) PauseMenu.Instance.Resume();
            else PauseMenu.Instance.Pause();
        }

        private void OpenSettings()
        {
            if (SettingsPanel.Instance == null) { Debug.LogWarning("[UITester] no SettingsPanel"); return; }
            SettingsPanel.Instance.Open();
        }

        private void FireGameOver()
        {
            GameOverController.TriggerGameOver("test (UIPanelTester)");
        }

        private void FireVictory()
        {
            VictoryController.TriggerVictory("test (UIPanelTester)");
        }

        private void FireFlashback()
        {
            if (FlashbackController.Instance == null) { Debug.LogWarning("[UITester] no FlashbackController"); return; }
            FlashbackController.Instance.PlayFrames();
        }

        private void ResetAll()
        {
            if (DialogueManager.Instance != null) DialogueManager.Instance.ClearAll();
            if (PauseMenu.Instance != null && PauseMenu.Instance.IsOpen) PauseMenu.Instance.Resume();
            GameOverController.ResetState();
            VictoryController.ResetState();
            Time.timeScale = 1f;
            Debug.Log("[UITester] reset");
        }
    }
}
