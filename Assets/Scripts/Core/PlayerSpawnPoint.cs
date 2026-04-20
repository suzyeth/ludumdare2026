using UnityEngine;
using UnityEngine.SceneManagement;
using PrismZone.Player;

namespace PrismZone.Core
{
    /// <summary>
    /// Place one or more of these in each scene. On scene start, the one whose
    /// <see cref="spawnId"/> matches SceneTransition.ConsumePendingSpawn() teleports
    /// the persistent Player there. If no pending id (e.g. first scene), the spawn
    /// point marked <see cref="isDefault"/>=true wins.
    /// </summary>
    public class PlayerSpawnPoint : MonoBehaviour
    {
        [SerializeField] private string spawnId = "default";
        [SerializeField] private bool isDefault = false;

        public string SpawnId => spawnId;
        public bool IsDefault => isDefault;

        // Per-scene-load flag: the first SpawnPoint that claims the Player flips this,
        // so later Start() calls (including the default) skip and don't re-teleport.
        private static int _claimedFrameForScene = -1;
        private static int _claimedScene = -1;

        /// <summary>
        /// RunState.ResetForNewRun hook: clears the static claim so the next scene's
        /// default spawn point isn't skipped because a prior run already "claimed" it.
        /// </summary>
        public static void ResetStaticState()
        {
            _claimedFrameForScene = -1;
            _claimedScene = -1;
        }

        private void OnEnable()
        {
            // Reset on fresh scene load so the next scene starts clean.
            int cur = SceneManager.GetActiveScene().buildIndex;
            if (_claimedScene != cur) { _claimedScene = cur; _claimedFrameForScene = -1; }
        }

        private void Start()
        {
            if (_claimedFrameForScene >= 0) return; // another spawn already took the player

            var wanted = SceneTransition.PeekPendingSpawn();
            bool matches = !string.IsNullOrEmpty(wanted) ? wanted == spawnId : isDefault;
            if (!matches) return;

            SceneTransition.ConsumePendingSpawn();
            _claimedFrameForScene = Time.frameCount;

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning($"[SpawnPoint '{spawnId}'] No Player in scene — expected persistent Player via DontDestroyOnLoad.");
                return;
            }
            player.transform.position = transform.position;
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
            Debug.Log($"[SpawnPoint] Placed player at '{spawnId}' {transform.position}");
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = isDefault ? new Color(0.4f, 1f, 0.4f, 0.8f) : new Color(1f, 1f, 0.3f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, 0.4f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 0.8f);
        }
    }
}
