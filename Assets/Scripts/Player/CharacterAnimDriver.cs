using UnityEngine;

namespace PrismZone.Player
{
    /// <summary>
    /// Drives an Animator that holds directional idle/walk clips but has no
    /// Parameters or Transitions wired. Reads the parent's Rigidbody2D velocity
    /// to pick the right clip and calls animator.Play() directly.
    /// Works with the art-team's Anm_Character01/02 controllers as-is.
    /// Attach to the GameObject that owns the Animator (same GO as the SpriteRenderer).
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class CharacterAnimDriver : MonoBehaviour
    {
        [Tooltip("Clip name prefix. Player: 'Ani_Character01_'  Guard: 'Ani_Character2_'")]
        [SerializeField] private string clipPrefix = "Ani_Character01_";
        [Tooltip("True = has back/front/side directional clips. False = just idle/walk.")]
        [SerializeField] private bool hasDirections = true;
        [Tooltip("Velocity magnitude below this counts as idle.")]
        [SerializeField] private float moveThreshold = 0.05f;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private Animator _animator;
        private Rigidbody2D _body;
        private string _facing = "front";

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _body = GetComponentInParent<Rigidbody2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            Vector2 v = _body != null ? _body.linearVelocity : Vector2.zero;
            bool moving = v.sqrMagnitude > moveThreshold * moveThreshold;

            if (hasDirections && moving)
            {
                if (Mathf.Abs(v.y) > Mathf.Abs(v.x))
                    _facing = v.y > 0f ? "back" : "front";
                else
                    _facing = "side";
            }

            // Side clip is drawn facing LEFT — mirror horizontally when moving right.
            // Only updates flipX while moving so idle retains the last walk facing.
            if (spriteRenderer != null && _facing == "side" && moving)
                spriteRenderer.flipX = v.x > 0f;

            string clip = hasDirections
                ? $"{clipPrefix}{_facing}_{(moving ? "walk" : "idle")}"
                : $"{clipPrefix}{(moving ? "walk" : "idle")}";

            // Animator.Play is a no-op when already in the target state, so this
            // is safe to call every frame.
            _animator.Play(clip);
        }
    }
}
