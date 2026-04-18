using UnityEngine;
using UnityEngine.SceneManagement;
using PrismZone.Player;

namespace PrismZone.Core
{
    /// <summary>
    /// Attach to a stair / exit door trigger. On OnTriggerEnter2D by the Player,
    /// stores the target spawn id then loads the target scene. The loaded scene's
    /// <see cref="PlayerSpawnPoint"/> components search for a matching id and
    /// teleport the player there on Start.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class SceneTransition : MonoBehaviour
    {
        [SerializeField] private string targetScene = "Scene_Corridor";
        [SerializeField] private string targetSpawnId = "default";
        [SerializeField] private float cooldownAfterEnter = 1.0f;

        private static string _pendingSpawnId;
        /// <summary>Read by PlayerSpawnPoint on scene load. Cleared after use.</summary>
        public static string ConsumePendingSpawn()
        {
            var s = _pendingSpawnId;
            _pendingSpawnId = null;
            return s;
        }

        private bool _triggered;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_triggered) return;
            if (!other.CompareTag("Player")) return;
            var pc = other.GetComponent<PlayerController>();
            if (pc != null && pc.IsHidden) return;

            _triggered = true;
            _pendingSpawnId = targetSpawnId;
            Debug.Log($"[SceneTransition] → {targetScene} spawn='{targetSpawnId}'");
            SceneManager.LoadSceneAsync(targetScene);
        }
    }
}
