using System.Collections;
using UnityEngine;
using PrismZone.UI;

namespace PrismZone.Interact
{
    /// <summary>
    /// Scripted beat chain: <see cref="triggerNodeId"/> finishes →
    /// <see cref="FlashbackController.PlayFrames"/> → <see cref="afterFlashbackNodeId"/>.
    /// Default: T-15 → flashback → T-17.
    ///
    /// Lives on a persistent scene-root object (the triggering Pickup / Trigger
    /// may destroy itself; this component isn't bound to their lifetime).
    /// </summary>
    public class FlashbackSequenceOrchestrator : MonoBehaviour
    {
        [Tooltip("TSV node id whose close kicks off the flashback. Default T-15.")]
        [SerializeField] private string triggerNodeId = "T-15";
        [Tooltip("TSV node id fired after the flashback completes. Default T-17.")]
        [SerializeField] private string afterFlashbackNodeId = "T-17";

        private bool _subscribed;
        private bool _fired;

        private IEnumerator Start()
        {
            while (DialogueManager.Instance == null) yield return null;
            DialogueManager.Instance.OnDialogueFinished += HandleFinished;
            _subscribed = true;
        }

        private void OnDestroy()
        {
            if (_subscribed && DialogueManager.Instance != null)
                DialogueManager.Instance.OnDialogueFinished -= HandleFinished;
        }

        private void HandleFinished(DialogueType type, string tag)
        {
            if (_fired) return;
            if (string.IsNullOrEmpty(triggerNodeId) || tag != triggerNodeId) return;
            _fired = true;
            StartCoroutine(RunSequence());
        }

        private IEnumerator RunSequence()
        {
            var fb = FlashbackController.Instance;
            if (fb != null)
            {
                bool done = false;
                fb.PlayFrames(() => done = true);
                while (!done) yield return null;
            }
            else
            {
                Debug.LogWarning("[FlashbackSequence] FlashbackController.Instance missing — skipping flashback");
            }

            if (!string.IsNullOrEmpty(afterFlashbackNodeId) && DialogueManager.Instance != null)
            {
                bool done = false;
                DialogueManager.Instance.ShowById(afterFlashbackNodeId, () => done = true);
                while (!done) yield return null;
            }
        }
    }
}
