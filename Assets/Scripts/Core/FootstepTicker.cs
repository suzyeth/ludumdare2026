using UnityEngine;

namespace PrismZone.Core
{
    /// <summary>
    /// Periodic footstep SFX for any body that moves via Rigidbody2D.linearVelocity.
    /// Attach to the sprite GO (or the Rigidbody2D owner), pick a SoundId + interval,
    /// and this component plays through AudioManager while velocity is above a
    /// threshold. Shared by Player (Footstep) and Guard (GuardFootstep).
    /// </summary>
    public class FootstepTicker : MonoBehaviour
    {
        [SerializeField] private SoundId soundId = SoundId.Footstep;
        [Tooltip("Time between steps at normal walk speed. Scaled by |velocity| vs walkReferenceSpeed.")]
        [SerializeField] private float baseInterval = 0.38f;
        [Tooltip("Reference speed used to normalize step cadence. Faster actual speed → shorter interval.")]
        [SerializeField] private float walkReferenceSpeed = 3f;
        [Tooltip("Below this |velocity| the ticker goes silent.")]
        [SerializeField] private float moveThreshold = 0.05f;
        [SerializeField] private Rigidbody2D body;

        private float _nextStepTime;

        private void Awake()
        {
            if (body == null) body = GetComponentInParent<Rigidbody2D>();
        }

        private void Update()
        {
            if (body == null) return;
            float v = body.linearVelocity.magnitude;
            if (v <= moveThreshold) { _nextStepTime = 0f; return; }
            if (Time.time < _nextStepTime) return;

            AudioManager.Instance?.Play(soundId);

            float scale = walkReferenceSpeed / Mathf.Max(v, 0.01f);
            _nextStepTime = Time.time + baseInterval * scale;
        }
    }
}
