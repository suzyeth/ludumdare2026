using UnityEngine;

namespace PrismZone.Player
{
    /// <summary>
    /// 2D camera follow with a rectangular dead zone. The camera only moves when
    /// the target leaves the dead zone, then hard-snaps to the edge so the target
    /// sits back inside. No SmoothDamp — combining it with pixel-snap produces a
    /// staircase judder in pixel-art games. Preserves the camera's Z; auto-finds
    /// the Player-tagged GameObject if <see cref="target"/> is empty.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [Tooltip("World units per pixel. PPU 32 → 1/32. 0 disables snap.")]
        [SerializeField] private float pixelUnit = 1f / 32f;
        [Tooltip("Extra offset from the target (e.g. (0, 0.5) pushes camera slightly up).")]
        [SerializeField] private Vector2 offset = new Vector2(0f, 0.5f);
        [Tooltip("Rectangle (width, height) centered on the camera. Target inside = camera stays still.")]
        [SerializeField] private Vector2 deadZoneSize = new Vector2(3f, 1.5f);

        private void Awake()
        {
            if (target == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) target = p.transform;
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;
            Vector2 targetPos = (Vector2)target.position + offset;
            Vector2 camPos = transform.position;
            Vector2 delta = targetPos - camPos;
            float halfX = deadZoneSize.x * 0.5f;
            float halfY = deadZoneSize.y * 0.5f;
            // Push camera only just enough to put the target back on the dead-zone edge.
            if (delta.x >  halfX) camPos.x = targetPos.x - halfX;
            if (delta.x < -halfX) camPos.x = targetPos.x + halfX;
            if (delta.y >  halfY) camPos.y = targetPos.y - halfY;
            if (delta.y < -halfY) camPos.y = targetPos.y + halfY;
            if (pixelUnit > 0f)
            {
                camPos.x = Mathf.Round(camPos.x / pixelUnit) * pixelUnit;
                camPos.y = Mathf.Round(camPos.y / pixelUnit) * pixelUnit;
            }
            transform.position = new Vector3(camPos.x, camPos.y, transform.position.z);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 1f, 0.2f, 0.35f);
            Gizmos.DrawWireCube(transform.position, new Vector3(deadZoneSize.x, deadZoneSize.y, 0.1f));
        }
    }
}
