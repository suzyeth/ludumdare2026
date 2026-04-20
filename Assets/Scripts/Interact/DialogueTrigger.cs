using UnityEngine;
using UnityEngine.Events;
using PrismZone.Core;
using PrismZone.Player;
using PrismZone.UI;

namespace PrismZone.Interact
{
    /// <summary>
    /// Scene-side wiring for AVG dialogue nodes (FRAMEWORK.md §5.1). Designer drops
    /// one of these per node, picks a <see cref="TriggerKind"/>, and the component
    /// dispatches the matching row from <see cref="TextTable"/> via
    /// <see cref="DialogueManager.ShowById"/>.
    ///
    /// Trigger kinds:
    ///   - OnEnter         : Player walks into the attached Collider2D (set IsTrigger).
    ///   - OnInteract      : Player presses E while in range (uses IInteractable).
    ///   - OnFilterChange  : Fires when the player's filter changes — combine with
    ///                       <see cref="condition"/> to require a specific color.
    ///   - OnState         : Re-evaluates whenever any GameFlag changes (drives
    ///                       follow-up beats like T-22 reacting to T-21 finishing).
    ///   - OnExternal      : Only fires when another script calls <see cref="Fire"/>.
    ///
    /// Gating uses <see cref="Condition"/>: list flags in requireAll / forbidAll /
    /// requireAny. The eyewear, inventory, and prior-dialogue gates of v0.4 are all
    /// expressed as flag keys (FlagKeys.Filter.Current_Red, FlagKeys.Inventory.Has_X,
    /// FlagKeys.Dialogue.T_05) — no special-case bools needed.
    ///
    /// One-shot is the default. Re-readable nodes (T-08 letter, T-11 notes) set
    /// <see cref="oneShot"/> = false; DialogueManager itself sets the
    /// dialogue.{id}.triggered flag on first fire so OnState chains stay one-shot.
    /// </summary>
    [AddComponentMenu("Prism Zone/Dialogue Trigger")]
    public class DialogueTrigger : MonoBehaviour, IInteractable
    {
        public enum TriggerKind { OnEnter, OnInteract, OnFilterChange, OnState, OnExternal }

        [Header("Identity")]
        [Tooltip("Story node id from text_table.tsv, e.g. T-01.")]
        [NodeIdDropdown]
        [SerializeField] private string nodeId;

        [Header("Trigger")]
        [SerializeField] private TriggerKind kind = TriggerKind.OnEnter;
        [SerializeField] private bool oneShot = true;

        [Header("Gating (per FRAMEWORK §3.3)")]
        [SerializeField] private Condition condition;

        [Header("READ Header (optional)")]
        [Tooltip("READ only — TSV id whose text becomes the popup title (e.g. 'item.diary.name').")]
        [SerializeField] private string titleKey;
        [Tooltip("READ only — sprite shown in the popup's left image slot (128×128).")]
        [SerializeField] private Sprite headerSprite;

        [Header("Interaction Prompt")]
        [SerializeField] private string promptKey = "ui.interact.prompt";

        [Header("On Finished")]
        [SerializeField] private UnityEvent onDialogueFinished;

        private bool _fired;

        public string PromptKey => promptKey;
        public string NodeId => nodeId;

        // --- Unity lifecycle --------------------------------------------------

        private void Awake()
        {
            // Cross-scene persistence guard: if this one-shot node already fired
            // in a previous visit (GameFlags records it permanently), a scene
            // reload spawning a fresh trigger instance must NOT re-fire. Without
            // this, OnState watchers like _T04_Trigger replay every time the
            // player re-enters Scn_3Floor because `_fired` resets with the instance.
            if (oneShot && !string.IsNullOrEmpty(nodeId)
                && GameFlags.Get($"dialogue.{nodeId}.triggered"))
            {
                _fired = true;
            }
        }

        private void OnEnable()
        {
            if (kind == TriggerKind.OnFilterChange)
                FilterManager.OnFilterChanged += HandleFilterChanged;
            if (kind == TriggerKind.OnState)
                GameFlags.OnChanged += HandleFlagChanged;
        }

        private void OnDisable()
        {
            FilterManager.OnFilterChanged -= HandleFilterChanged;
            GameFlags.OnChanged -= HandleFlagChanged;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (kind != TriggerKind.OnEnter) return;
            if (!other.CompareTag("Player")) return;
            TryFire();
        }

        // --- IInteractable ----------------------------------------------------

        public bool CanInteract(GameObject who)
        {
            if (kind != TriggerKind.OnInteract) return false;
            if (_fired && oneShot) return false;
            return condition.Evaluate();
        }

        public void Interact(GameObject who)
        {
            if (kind != TriggerKind.OnInteract) return;
            TryFire();
        }

        // --- External / state-change paths ------------------------------------

        /// <summary>For OnExternal kind (or scripted calls) — bypass kind check.</summary>
        public void Fire() => TryFire(forceFromExternal: true);

        private void HandleFilterChanged(FilterColor prev, FilterColor next) => TryFire();

        // OnState: any flag write re-checks the condition. Cheap because Evaluate
        // is just a few dictionary lookups; oneShot guards stop the flood once fired.
        private void HandleFlagChanged(string key) => TryFire();

        // --- Core fire --------------------------------------------------------

        private void TryFire(bool forceFromExternal = false)
        {
            if (_fired && oneShot) return;
            if (!forceFromExternal && !condition.Evaluate()) return;
            if (string.IsNullOrEmpty(nodeId))
            {
                Debug.LogWarning($"[DialogueTrigger:{name}] No nodeId configured.");
                return;
            }
            if (DialogueManager.Instance == null)
            {
                Debug.LogWarning($"[DialogueTrigger:{nodeId}] No DialogueManager in scene.");
                return;
            }

            // ENV nodes anchor to this transform (popup positions itself in world space).
            var entry = TextTable.Get(nodeId);
            Vector3? pos = entry != null && entry.avgType == TextEntry.AvgType.ENV
                ? (Vector3?)transform.position : null;

            DialogueManager.Instance.ShowById(nodeId, OnFinished, pos, titleKey, headerSprite);
            _fired = true;
        }

        private void OnFinished() => onDialogueFinished?.Invoke();

        /// <summary>Designer hook to manually re-arm a oneShot trigger (e.g. New Game).</summary>
        public void Rearm() { _fired = false; }
    }
}
