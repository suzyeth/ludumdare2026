using System;
using UnityEngine;
using PrismZone.Player;

namespace PrismZone.Interact
{
    /// <summary>
    /// 4-digit passcode door. Opening raises OnRequestPasscode; UI hosts the panel
    /// and calls TrySubmit with the user input. On success, the door collider turns
    /// off and the sprite swaps to Open.
    /// </summary>
    public class PasscodeDoor : MonoBehaviour, IInteractable
    {
        [SerializeField] private string passcode = "3071";
        [SerializeField] private Collider2D blockingCollider;
        [SerializeField] private SpriteRenderer doorRenderer;
        [SerializeField] private Sprite closedSprite;
        [SerializeField] private Sprite openSprite;
        [SerializeField] private string promptKey = "ui.passcode.title";

        public bool IsOpen { get; private set; }

        public string PromptKey => promptKey;

        /// <summary>Raised when player presses E at the door. UI listens and opens the passcode panel.</summary>
        public static event Action<PasscodeDoor> OnRequestPasscode;

        public bool CanInteract(GameObject who) => !IsOpen;

        public void Interact(GameObject who)
        {
            if (IsOpen) return;
            OnRequestPasscode?.Invoke(this);
        }

        public bool TrySubmit(string attempt)
        {
            if (attempt != passcode) return false;
            Open();
            return true;
        }

        public void Open()
        {
            IsOpen = true;
            if (blockingCollider != null) blockingCollider.enabled = false;
            if (doorRenderer != null && openSprite != null) doorRenderer.sprite = openSprite;
        }
    }
}
