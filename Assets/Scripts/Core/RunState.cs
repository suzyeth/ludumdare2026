using UnityEngine;

namespace PrismZone.Core
{
    /// <summary>
    /// Centralises "new game" state scrubbing so the next run doesn't inherit
    /// dialogue flags, inventory, filter color, or scene-transition intent from
    /// the previous run. Pickup.Awake and DialogueTrigger.Awake both read the
    /// persistent state to self-lock on scene reload mid-run — without this
    /// scrub, clicking "New Game" after finishing a run would have every pickup
    /// self-destruct on load and every one-shot trigger pre-fire.
    ///
    /// Call sites:
    ///   - MainMenu.StartGame — before LoadSceneAsync(firstScene)
    ///   - any future "restart from death screen" flow
    /// </summary>
    public static class RunState
    {
        public static void ResetForNewRun()
        {
            GameFlags.Clear();

            if (PrismZone.UI.Inventory.Instance != null)
                PrismZone.UI.Inventory.Instance.Clear();

            if (FilterManager.Instance != null)
                FilterManager.Instance.SetFilter(FilterColor.None);

            SceneTransition.SetPendingSpawn(null);
            PlayerSpawnPoint.ResetStaticState();

            Debug.Log("[RunState] Cleared for new run.");
        }
    }
}
