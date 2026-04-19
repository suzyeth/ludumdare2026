using UnityEngine;
using UnityEngine.InputSystem;
using PrismZone.Core;
using PrismZone.UI;

namespace PrismZone.Player
{
    /// <summary>
    /// Side-view walking controller with ladder-zone vertical movement (no jump).
    /// Reads the project's InputSystem_Actions asset. Emits sound pulses when running
    /// for RED enemy detection via PlayerNoise.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 3f;
        [SerializeField] private float runSpeed = 5f;
        [SerializeField] private float climbSpeed = 2.5f;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [Tooltip("Pixels per unit. Used for LateUpdate pixel-snap.")]
        [SerializeField] private float pixelsPerUnit = 32f;

        [Header("Noise (RED detection)")]
        [SerializeField] private float runNoiseInterval = 0.25f;
        [SerializeField] private float runNoiseRadius = 8f;

        private Rigidbody2D _rb;
        private Vector2 _moveInput;
        private bool _runHeld;
        private int _ladderZones;
        private float _nextNoiseTime;

        public bool IsHidden { get; set; }
        public bool IsRunning => _runHeld && _moveInput.sqrMagnitude > 0.01f;
        public Vector2 Velocity => _rb != null ? _rb.linearVelocity : Vector2.zero;

        private InputAction _moveAction;
        private InputAction _runAction;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void OnEnable()
        {
            BindInputs();
        }

        private void OnDisable()
        {
            UnbindInputs();
        }

        private void BindInputs()
        {
            // Move/Sprint go through the project's InputActions asset (bindings already in
            // the template). Filter hotkeys + Pause read Keyboard.current directly — cheaper
            // than authoring 5 more actions, and immune to asset churn when Unity rewrites
            // the InputActions JSON on save.
            _moveAction = InputSystem.actions?.FindAction("Move");
            _runAction  = InputSystem.actions?.FindAction("Sprint") ?? InputSystem.actions?.FindAction("Run");
        }

        private void UnbindInputs() { /* shared asset, no enable/disable toggling needed */ }

        private void Update()
        {
            ReadInput();
            HandleFilterHotkeys();
            HandleRunningNoise();
        }

        private void FixedUpdate()
        {
            // v1.2 spec §4.3: during a broadcast the player is stunned (cannot move).
            // BroadcastController flips this static while a broadcast is mid-cycle.
            if (IsHidden || BroadcastController.IsBroadcasting) { _rb.linearVelocity = Vector2.zero; return; }

            // Free 2D movement by default. Ladder zones can still cap vertical speed
            // to climbSpeed, but otherwise W/S move full-speed so the MVP single-floor
            // room plays like a top-down explorer.
            float speed = _runHeld ? runSpeed : walkSpeed;
            float vx = _moveInput.x * speed;
            float vy = _ladderZones > 0
                ? _moveInput.y * climbSpeed
                : _moveInput.y * speed;
            _rb.linearVelocity = new Vector2(vx, vy);
            // flipX is now owned by CharacterAnimDriver (which knows whether
            // the current clip is the side-view frame that needs mirroring).
        }

        // LateUpdate pixel snap removed in v0.4 — Pixel Perfect Camera handles
        // whole-pixel alignment. Manual snap fights PPC's upscale RT path.

        private void ReadInput()
        {
            _moveInput = _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
            _runHeld = _runAction != null && _runAction.IsPressed();
        }

        private const string GlassesItemId = "item.glasses";

        private void HandleFilterHotkeys()
        {
            if (FilterManager.Instance == null) return;
            var kb = Keyboard.current;
            if (kb == null) return;

            // v1.2 spec: Q cycles through None → Red → Green → None.
            // Glasses must be in inventory to engage a color; without glasses
            // Q still allows returning to None so any stuck state is resettable.
            if (!kb.qKey.wasPressedThisFrame) return;

            bool hasGlasses = Inventory.Instance != null && Inventory.Instance.Has(GlassesItemId);
            if (!hasGlasses)
            {
                FilterManager.Instance.SetFilter(FilterColor.None);
                return;
            }
            FilterManager.Instance.CycleLens();
        }

        private void HandleRunningNoise()
        {
            if (!IsRunning || IsHidden) return;
            if (Time.time < _nextNoiseTime) return;
            _nextNoiseTime = Time.time + runNoiseInterval;
            PlayerNoise.Emit(transform.position, runNoiseRadius);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Ladder")) _ladderZones++;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Ladder")) _ladderZones = Mathf.Max(0, _ladderZones - 1);
        }
    }
}
