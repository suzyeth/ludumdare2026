using UnityEngine;
using PrismZone.Core;
using PrismZone.Player;

namespace PrismZone.Interact
{
    /// <summary>
    /// Attach to a stair / exit-door trigger. Two modes:
    ///   - If <see cref="targetScene"/> is set: loads that scene (same as SceneTransition).
    ///   - Otherwise: fires <see cref="VictoryController.TriggerVictory"/> — the final
    ///     escape the player is trying to reach.
    /// Respects Player's IsHidden — you can't "win from a cabinet".
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ExitTrigger : MonoBehaviour
    {
        [Tooltip("Leave blank to treat this as the final victory trigger.")]
        [SerializeField] private string targetScene;
        [SerializeField] private string targetSpawnId = "default";
        [SerializeField] private string victoryReason = "escaped";

        [Tooltip("Unique id for persistent fire-once tracking across scene reloads. Leave blank to disable persistence (trigger resets each load).")]
        [SerializeField] private string exitId;

        private bool _fired;

        private string FlagKey => string.IsNullOrEmpty(exitId) ? null : $"exit.{exitId}.fired";

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;

            // If a persistent exitId is configured and this trigger already fired in
            // a previous scene visit, suppress it immediately so a scene reload does
            // not double-load or double-trigger victory.
            var key = FlagKey;
            if (key != null && GameFlags.Get(key)) _fired = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_fired) return;
            if (!other.CompareTag("Player")) return;
            var pc = other.GetComponent<PlayerController>();
            if (pc != null && pc.IsHidden) return;
            _fired = true;
            var key = FlagKey;
            if (key != null) GameFlags.Set(key, true);

            if (!string.IsNullOrEmpty(targetScene))
            {
                SceneTransition.SetPendingSpawn(targetSpawnId);
                UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(targetScene);
            }
            else
            {
                VictoryController.TriggerVictory(victoryReason);
            }
        }
    }
}
