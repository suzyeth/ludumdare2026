using System;
using UnityEngine;
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
        [SerializeField] private string promptKeyOpen = "ui.door.open";

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
                    if (CluePopup.Instance != null)
                        CluePopup.Instance.Show(promptKeyLocked);
                    return;
                }
                if (consumeKey) inv.Remove(requiredKeyId);
            }

            Open();
        }

        public void Open()
        {
            IsOpen = true;
            if (blockingCollider != null) blockingCollider.enabled = false;
            if (doorRenderer != null && openSprite != null) doorRenderer.sprite = openSprite;
            PrismZone.Core.AudioManager.Instance?.Play(PrismZone.Core.SoundId.DoorUnlock);
        }
    }
}
