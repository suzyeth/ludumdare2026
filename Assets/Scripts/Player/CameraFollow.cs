using UnityEngine;

namespace PrismZone.Player
{
    /// <summary>
    /// 2D camera follow. Tracks a target (usually the player) each LateUpdate,
    /// preserves the camera's Z, and snaps to pixel boundaries so the image
    /// plays nicely with PixelPerfectCamera. Auto-finds the Player-tagged
    /// GameObject if <see cref="target"/> is not assigned in the Inspector.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [Tooltip("Smoothing time. 0 = instant snap, higher = laggier follow.")]
        [SerializeField] private float smoothTime = 0.08f;
        [Tooltip("World units per pixel. PPU 32 → 1/32. 0 disables snap.")]
        [SerializeField] private float pixelUnit = 1f / 32f;
        [Tooltip("Offset in world units — use to shift the camera above the player, etc.")]
        [SerializeField] private Vector2 offset = Vector2.zero;

        private Vector3 _velocity;

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
            Vector3 desired = new Vector3(
                target.position.x + offset.x,
                target.position.y + offset.y,
                transform.position.z);
            Vector3 result = smoothTime > 0f
                ? Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothTime)
                : desired;
            if (pixelUnit > 0f)
            {
                result.x = Mathf.Round(result.x / pixelUnit) * pixelUnit;
                result.y = Mathf.Round(result.y / pixelUnit) * pixelUnit;
            }
            transform.position = result;
        }
    }
}
