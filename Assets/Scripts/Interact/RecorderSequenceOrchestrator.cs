using System.Collections;
using UnityEngine;
using PrismZone.Core;
using PrismZone.Enemy;
using PrismZone.UI;

namespace PrismZone.Interact
{
    /// <summary>
    /// Scripted beat chain fired when the player picks up the recorder.
    ///
    /// Flow:
    ///   1. <see cref="pickupReadNodeId"/> (item.recorder READ) closes
    ///   2. Broadcast plays (prelude + loop) via BroadcastController.TriggerNow
    ///   3. Broadcast ends naturally
    ///   4. <see cref="afterBroadcastNodeId"/> (T-18) fires
    ///   5. <see cref="afterT18NodeId"/> (T-19) fires
    ///   6. All enemies → Stopped; broadcast permanently disarmed
    ///   7. <see cref="dadNpc"/> (or first enemy found) teleports to
    ///      <see cref="dadRevealAnchor"/>.position
    ///   8. <see cref="afterT19NodeId"/> (T-20) fires
    ///   9. The NPC GameObject turns off (dad reveal completes)
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
        [SerializeField] private string afterT19NodeId = "T-20";

        [Header("Dad Reveal (between T-19 and T-20)")]
        [Tooltip("Empty Transform in the scene marking where the dad NPC appears. Drop an empty GameObject at the reveal spot and wire it here.")]
        [SerializeField] private Transform dadRevealAnchor;
        [Tooltip("Which enemy GameObject to teleport + reveal as 'dad'. Leave null to grab the first enemy in EnemyBase.All (usually Guard_NPC).")]
        [SerializeField] private GameObject dadNpc;
        [Tooltip("If true, the dad NPC GameObject is SetActive(false) after T-20 closes (image disappears). If false, the NPC stays visible but frozen.")]
        [SerializeField] private bool hideNpcAfterT20 = true;
        [Tooltip("If true, every enemy in the scene (not just the dad NPC) is SetActive(false) after T-20 closes. Use this when the story says all guards vanish.")]
        [SerializeField] private bool hideAllEnemiesAfterT20 = true;

        private bool _subscribed;
        private bool _fired;

        private IEnumerator Start()
        {
            while (DialogueManager.Instance == null) yield return null;
            DialogueManager.Instance.OnDialogueFinished += HandleFinished;
            _subscribed = true;

            // Cross-scene / late-bind recovery: if the pickup READ already
            // fired before this orchestrator was alive (scene load after
            // pickup, hot-reload during testing), the event is gone. Peek at
            // the persistent GameFlag and kick the chain manually.
            if (!_fired
                && !string.IsNullOrEmpty(pickupReadNodeId)
                && GameFlags.Get($"dialogue.{pickupReadNodeId}.triggered"))
            {
                _fired = true;
                StartCoroutine(RunSequence());
            }
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
                yield return null;
                yield return null;
                while (BroadcastController.IsBroadcasting) yield return null;
            }

            // 2. T-18
            yield return ShowDialogueAndWait(afterBroadcastNodeId);

            // 3. T-19
            yield return ShowDialogueAndWait(afterT18NodeId);

            // 4. Freeze the world.
            if (BroadcastController.Instance != null)
                BroadcastController.Instance.DisarmPermanent();
            foreach (var e in EnemyBase.All)
            {
                if (e == null) continue;
                e.RequestState(EnemyBase.State.Stopped);
            }

            // 5. Teleport the designated NPC to the reveal anchor.
            //    Stopped state already prevents movement (GreenEnemy.Tick has no
            //    Stopped case → no movement code runs). The teleport is
            //    effectively permanent until T-20 finishes.
            var go = ResolveDadNpc();
            if (go != null && dadRevealAnchor != null)
            {
                go.transform.position = dadRevealAnchor.position;
                go.SetActive(true);
            }

            // 6. T-20
            yield return ShowDialogueAndWait(afterT19NodeId);

            // 7. NPC image disappears (spec: 'NPC 图像消失').
            if (go != null && hideNpcAfterT20) go.SetActive(false);

            // 8. Wipe every other guard off the floor too — T-20 reveal ends
            //    the "haunting" layer of the level, no enemies should remain
            //    visible afterward. Snapshot the registry first because
            //    SetActive(false) triggers OnDisable → All.Remove, which would
            //    mutate the collection we're iterating.
            if (hideAllEnemiesAfterT20)
            {
                var snapshot = new System.Collections.Generic.List<EnemyBase>(EnemyBase.All);
                foreach (var e in snapshot)
                {
                    if (e == null) continue;
                    if (go != null && e.gameObject == go) continue; // already handled
                    e.gameObject.SetActive(false);
                }
            }
        }

        private GameObject ResolveDadNpc()
        {
            if (dadNpc != null) return dadNpc;
            foreach (var e in EnemyBase.All)
            {
                if (e != null) return e.gameObject;
            }
            return null;
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
