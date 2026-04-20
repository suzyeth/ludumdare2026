using System;
using UnityEngine;
using UnityEngine.Events;
using PrismZone.Core;
using PrismZone.Player;
using PrismZone.UI;

namespace PrismZone.Interact
{
    /// <summary>
    /// Lockable door that opens when the player's inventory contains the matching
    /// <see cref="requiredKeyId"/> (e.g. "item.key.a"). Consumes the key by default.
    ///
    /// Differs from <see cref="PasscodeDoor"/>: no input panel, instant open on E.
    /// </summary>
    public class KeyDoor : MonoBehaviour, IInteractable
    {
        [SerializeField] private string requiredKeyId = "item.key.a";
        [SerializeField] private bool consumeKey = true;
        [SerializeField] private Collider2D blockingCollider;
        [SerializeField] private SpriteRenderer doorRenderer;
        [SerializeField] private Sprite closedSprite;
        [SerializeField] private Sprite openSprite;
        [SerializeField] private string promptKeyLocked = "ui.door.locked";
        [Tooltip("Shown when the player holds at least one key (item.key.*) but not the required one — disambiguates 'empty pocket' from 'wrong key in hand'.")]
        [SerializeField] private string promptKeyWrongKey = "ui.door.wrong_key";
        [SerializeField] private string promptKeyOpen = "ui.door.open";

        [Header("Audio")]
        [Tooltip("Which sound to play when this specific door opens. Default = DoorOpen (classroom wooden door). Set to GateOpen on stair / exit doors.")]
        [SerializeField] private SoundId openSoundId = SoundId.DoorOpen;

        [Header("On Open")]
        [Tooltip("Fired the instant the door unlocks. Wire scene objects here to reveal hidden interactables (e.g. Cabinet_Locked → Pickup_Glasses.SetActive).")]
        [SerializeField] private UnityEvent onOpen;

        [Header("Locked Dialogue")]
        [Tooltip("Optional TSV node id to fire the first time the player tries E on this door without the key. Once fired, subsequent locked attempts fall back to the CluePopup hint. Leave empty to always use the hint.")]
        [NodeIdDropdown]
        [SerializeField] private string lockedDialogueNodeId;

        [Header("Open Dialogue")]
        [Tooltip("Optional TSV node id to fire the instant the door successfully opens (e.g. 404 classroom door → T-14). Runs after onOpen, once per run since the door stays open.")]
        [NodeIdDropdown]
        [SerializeField] private string openDialogueNodeId;

        public bool IsOpen { get; private set; }

        // Plain doors (empty requiredKeyId) must NOT show the "locked" prompt — show
        // the generic interact hint so the UI matches behaviour (press E, it opens).
        public string PromptKey => IsOpen
            ? promptKeyOpen
            : (string.IsNullOrEmpty(requiredKeyId) ? "ui.interact.prompt" : promptKeyLocked);

        public bool CanInteract(GameObject who) => !IsOpen;

        public void Interact(GameObject who)
        {
            if (IsOpen) return;

            // Empty requiredKeyId = plain door, opens on E with no key gate.
            if (!string.IsNullOrEmpty(requiredKeyId))
            {
                var inv = Inventory.Instance;
                if (inv == null || !inv.Has(requiredKeyId))
                {
                    PrismZone.Core.AudioManager.Instance?.Play(PrismZone.Core.SoundId.DoorLocked);
                    // First locked press on a door wired with a dialogue → play the NAR
                    // beat (atmospheric intro for the door). Subsequent presses fall back
                    // to the short CluePopup hint so the player isn't forced to re-read.
                    bool usedDialogue = false;
                    if (!string.IsNullOrEmpty(lockedDialogueNodeId)
                        && DialogueManager.Instance != null
                        && !GameFlags.Get($"dialogue.{lockedDialogueNodeId}.triggered"))
                    {
                        DialogueManager.Instance.ShowById(lockedDialogueNodeId);
                        usedDialogue = true;
                    }
                    if (!usedDialogue && CluePopup.Instance != null)
                    {
                        // If the player is carrying any key (item.key.*) just not the
                        // matching one, surface a different hint so they know to try a
                        // different key rather than hunt for one they already own.
                        string hint = (inv != null && HasAnyKey(inv))
                            ? promptKeyWrongKey
                            : promptKeyLocked;
                        CluePopup.Instance.Show(hint);
                    }
                    return;
                }
                if (consumeKey) inv.Remove(requiredKeyId);
            }

            Open();
        }

        private static bool HasAnyKey(Inventory inv)
        {
            foreach (var id in inv.Slots)
                if (!string.IsNullOrEmpty(id) && id.StartsWith("item.key.")) return true;
            return false;
        }

        public void Open()
        {
            IsOpen = true;
            if (blockingCollider != null) blockingCollider.enabled = false;
            if (doorRenderer != null && openSprite != null) doorRenderer.sprite = openSprite;
            AudioManager.Instance?.Play(SoundId.DoorUnlock);
            AudioManager.Instance?.Play(openSoundId);
            onOpen?.Invoke();
            if (!string.IsNullOrEmpty(openDialogueNodeId) && DialogueManager.Instance != null)
                DialogueManager.Instance.ShowById(openDialogueNodeId);
        }
    }
}
