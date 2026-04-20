using System.Collections;
using UnityEngine;
using PrismZone.Core;
using PrismZone.UI;

namespace PrismZone.Interact
{
    /// <summary>
    /// Scripted beat chain fired when the player picks up the recorder:
    ///
    ///   1. <see cref="pickupReadNodeId"/> (item.recorder READ) closes
    ///   2. <see cref="BroadcastController.TriggerNow"/> — plays prelude + broadcast loop
    ///   3. Broadcast ends naturally (IsBroadcasting flips back to false)
    ///   4. <see cref="afterBroadcastNodeId"/> (T-18) fires
    ///   5. <see cref="afterT18NodeId"/> (T-19) fires
    ///
    /// Lives on a persistent scene-root object (not the pickup itself — the Pickup
    /// destroys its own GameObject on consume).
    /// </summary>
    public class RecorderSequenceOrchestrator : MonoBehaviour
    {
        [Tooltip("TSV node id of the recorder's READ popup. Fires the chain when this closes.")]
        [SerializeField] private string pickupReadNodeId = "item.recorder.page.1";
        [SerializeField] private string afterBroadcastNodeId = "T-18";
        [SerializeField] private string afterT18NodeId = "T-19";

        private bool _subscribed;
        private bool _fired;

        private IEnumerator Start()
        {
            // DialogueManager may come online a frame after scene load; retry until ready.
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
            if (string.IsNullOrEmpty(pickupReadNodeId) || tag != pickupReadNodeId) return;
            _fired = true;
            StartCoroutine(RunSequence());
        }

        private IEnumerator RunSequence()
        {
            // 1. Broadcast
            var bc = BroadcastController.Instance;
            if (bc != null)
            {
                bc.TriggerNow();
                // Wait a frame so IsBroadcasting latches true before we poll.
                yield return null;
                yield return null;
                while (BroadcastController.IsBroadcasting) yield return null;
            }

            // 2. T-18
            yield return ShowDialogueAndWait(afterBroadcastNodeId);

            // 3. T-19
            yield return ShowDialogueAndWait(afterT18NodeId);
        }

        private IEnumerator ShowDialogueAndWait(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId) || DialogueManager.Instance == null) yield break;
            bool done = false;
            DialogueManager.Instance.ShowById(nodeId, () => done = true);
            while (!done) yield return null;
        }
    }
}
