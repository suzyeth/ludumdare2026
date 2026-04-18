using UnityEngine;
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

        private void Start()
        {
            var wanted = SceneTransition.ConsumePendingSpawn();
            bool matches = !string.IsNullOrEmpty(wanted) ? wanted == spawnId : isDefault;
            if (!matches) return;

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
