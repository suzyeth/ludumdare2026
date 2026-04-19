using UnityEngine;
using UnityEngine.InputSystem;
using PrismZone.UI;

namespace PrismZone.UI
{
    /// <summary>
    /// Smoke-test shortcut for the AVG dialogue system. Drop onto any GameObject
    /// in a scene that also has HUD_Canvas + DialogueManager. Press F1-F4 at
    /// runtime to trigger the 4 popup styles. Delete this component once the
    /// 24 real DialogueTrigger nodes are wired.
    /// </summary>
    public class DebugDialogueTester : MonoBehaviour
    {
        [Tooltip("i18n key used for all four debug messages. Resolves to the key itself when missing.")]
        [SerializeField] private string debugKey = "ui.popup.next_hint";

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            var mgr = DialogueManager.Instance;
            if (mgr == null) return;

            if (kb.f1Key.wasPressedThisFrame)
                mgr.Show(DialogueType.NAR, debugKey, null, "DEBUG-NAR");
            if (kb.f2Key.wasPressedThisFrame)
                mgr.ShowKeys(DialogueType.READ,
                    new[] { debugKey, debugKey, debugKey }, null, null,
                    "DEBUG-READ", debugKey, null);
            if (kb.f3Key.wasPressedThisFrame)
                mgr.Show(DialogueType.FLASH, debugKey, null, "DEBUG-FLASH");
            if (kb.f4Key.wasPressedThisFrame)
                mgr.ShowEnv(debugKey, transform.position, null, "DEBUG-ENV");
        }
    }
}
