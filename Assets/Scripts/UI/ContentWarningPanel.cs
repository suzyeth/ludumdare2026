using UnityEngine;
using UnityEngine.InputSystem;

namespace PrismZone.UI
{
    /// <summary>
    /// Full-screen overlay that shows a content-warning notice before the main
    /// menu is usable. Dismissed by any key / mouse button press.
    ///
    /// Lives inside Scene_MainMenu so it surfaces on every boot (and every
    /// return to the main menu). Blocks interaction via its own CanvasGroup
    /// and a raycast-target backdrop image — the MainMenu buttons underneath
    /// stay unclickable until this closes.
    /// </summary>
    public class ContentWarningPanel : MonoBehaviour
    {
        [Tooltip("Seconds before input is accepted — stops a still-held key from the previous scene instantly dismissing the warning on show.")]
        [SerializeField] private float inputLockSeconds = 0.35f;

        private float _openedAt;
        private bool _dismissed;

        private void OnEnable()
        {
            _openedAt = Time.unscaledTime;
            _dismissed = false;
        }

        private void Update()
        {
            if (_dismissed) return;
            if (Time.unscaledTime - _openedAt < inputLockSeconds) return;

            var kb = Keyboard.current;
            var mouse = Mouse.current;
            bool pressed =
                (kb != null && kb.anyKey.wasPressedThisFrame) ||
                (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame));
            if (!pressed) return;

            _dismissed = true;
            gameObject.SetActive(false);
        }
    }
}
