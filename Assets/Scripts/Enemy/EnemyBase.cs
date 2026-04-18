using UnityEngine;
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
        public enum State { Idle, Patrol, Chase, Return }

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

        protected virtual void OnEnable() { FilterManager.OnFilterChanged += HandleFilterChanged; }
        protected virtual void OnDisable() { FilterManager.OnFilterChanged -= HandleFilterChanged; }

        protected virtual void Update() { Tick(); ApplyVisibility(); }

        protected abstract void Tick();

        protected virtual bool ForceVisibleDuringChase => true;

        private void HandleFilterChanged(FilterColor prev, FilterColor next) => ApplyVisibility();

        protected void ApplyVisibility()
        {
            if (spriteRenderer == null) return;
            bool revealed = FilterManager.Instance != null && FilterManager.Instance.Current == revealFilter;
            bool chaseVisible = ForceVisibleDuringChase && Current == State.Chase;
            float a = revealed ? revealedAlpha : chaseVisible ? chaseAlpha : hiddenAlpha;
            var c = spriteRenderer.color;
            c.a = a;
            spriteRenderer.color = c;
        }

        protected void SetState(State next)
        {
            if (Current == next) return;
            var prev = Current;
            Current = next;
            OnStateChanged(prev, next);
        }

        protected virtual void OnStateChanged(State prev, State next) { }
    }
}
