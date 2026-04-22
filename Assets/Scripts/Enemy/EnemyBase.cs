using UnityEngine;
using UnityEngine.SceneManagement;
using PrismZone.Core;

namespace PrismZone.Enemy
{
    /// <summary>
    /// Shared state machine scaffolding for all enemies. Subclasses override Tick().
    /// The reveal-filter wiring lives here so every enemy obeys the same rule:
    /// alpha = 1 when FilterManager.Current == RevealFilter OR stateVisible == true,
    /// otherwise near-zero (hidden but still drawn for masking).
    /// </summary>
    public abstract class EnemyBase : MonoBehaviour
    {
        // v1.2 spec §4.3 enemy state machine:
        //   Idle / Patrol — default ambient
        //   Alert         — heard noise / saw player in vision cone (no chase yet)
        //   Chase         — locked onto player; AVG popups force-close (DialogueManager listens)
        //   Locating      — broadcast period: rapid room-locate; AVG frozen, player stunned
        //   Return        — lost LOS; returning to patrol
        //   Stopped       — absorbing terminal; recorder turned off (T-19) → never chases again
        public enum State { Idle, Patrol, Alert, Chase, Locating, Return, Stopped }

        /// <summary>
        /// Fires whenever any enemy in the scene transitions between states.
        /// DialogueManager subscribes to this so AVG popups force-close when
        /// anyone enters Chase (spec §4.3). Cleared automatically on scene unload.
        /// </summary>
        public static event System.Action<EnemyBase, State, State> OnAnyStateChanged;

        // Fix 4: static event never cleared across scene loads — leaked subscribers
        // cause ghost callbacks from destroyed enemies. Register the cleanup once at
        // domain startup so it fires on every subsequent scene unload.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterSceneCleanup()
        {
            SceneManager.sceneUnloaded += _ => OnAnyStateChanged = null;
        }

        /// <summary>
        /// Live registry so orchestration code (BroadcastController, Recorder) can
        /// fan a state change to every active enemy without scene-scanning every frame.
        /// </summary>
        public static readonly System.Collections.Generic.HashSet<EnemyBase> All = new();

        [Header("Base")]
        [SerializeField] protected FilterColor revealFilter = FilterColor.None;
        [SerializeField] protected SpriteRenderer spriteRenderer;
        [SerializeField] protected float hiddenAlpha = 0.0f;
        [SerializeField] protected float chaseAlpha = 1.0f;
        [SerializeField] protected float revealedAlpha = 1.0f;

        public FilterColor RevealFilter => revealFilter;
        public State Current { get; protected set; } = State.Idle;

        protected Vector2 _homePosition;

        protected virtual void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            _homePosition = transform.position;
        }

        protected virtual void OnEnable()
        {
            FilterManager.OnFilterChanged += HandleFilterChanged;
            All.Add(this);
        }
        protected virtual void OnDisable()
        {
            FilterManager.OnFilterChanged -= HandleFilterChanged;
            All.Remove(this);
        }

        protected virtual void Update() { Tick(); ApplyVisibility(); }

        protected abstract void Tick();

        protected virtual bool ForceVisibleDuringChase => true;

        private void HandleFilterChanged(FilterColor prev, FilterColor next) => ApplyVisibility();

        protected void ApplyVisibility()
        {
            if (spriteRenderer == null) return;
            // No reveal filter assigned = enemy is always fully visible.
            // v1.2 main guard does not play the color-filter reveal game; only the
            // RED/GREEN/BLUE reserves (post-jam) use the filter-gated reveal.
            if (revealFilter == FilterColor.None) return;
            bool revealed = FilterManager.Instance != null && FilterManager.Instance.Current == revealFilter;
            bool chaseVisible = ForceVisibleDuringChase && Current == State.Chase;
            float a = revealed ? revealedAlpha : chaseVisible ? chaseAlpha : hiddenAlpha;
            var c = spriteRenderer.color;
            c.a = a;
            spriteRenderer.color = c;
        }

        /// <summary>
        /// Public entry for orchestration code (BroadcastController pushes all enemies
        /// into Locating; Recorder pushes them into Stopped). Subclasses still drive
        /// their own AI through the protected SetState.
        /// </summary>
        public void RequestState(State next) => SetState(next);

        protected void SetState(State next)
        {
            if (Current == next) return;
            // Stopped is absorbing: once the recorder is off, the enemy never
            // re-enters chase/patrol. Callers that miss this get silently ignored.
            if (Current == State.Stopped) return;
            var prev = Current;
            Current = next;
            // Entering Chase always stings the player; subclasses can elaborate.
            if (next == State.Chase && prev != State.Chase)
                AudioManager.Instance?.Play(SoundId.GuardSpot);
            OnStateChanged(prev, next);
            OnAnyStateChanged?.Invoke(this, prev, next);
        }

        protected virtual void OnStateChanged(State prev, State next) { }
    }
}
