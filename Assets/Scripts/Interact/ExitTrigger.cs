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

        private bool _fired;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_fired) return;
            if (!other.CompareTag("Player")) return;
            var pc = other.GetComponent<PlayerController>();
            if (pc != null && pc.IsHidden) return;
            _fired = true;

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
