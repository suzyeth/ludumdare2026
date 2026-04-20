using System.Collections;
using UnityEngine;
using PrismZone.UI;

namespace PrismZone.Core
{
    /// <summary>
    /// Fires a one-shot dialogue node when the scene starts. Designed for the
    /// opening monologue (T-01) and any other "as soon as the level loads" beats.
    ///
    /// Why a dedicated component instead of a <c>DialogueTrigger(OnEnter)</c>?
    ///   - No dependency on Player spawn order / Collider2D overlap timing.
    ///   - Works even when the player briefly isn't tagged "Player" (e.g. during
    ///     a cutscene where the player object is swapped out).
    ///   - Deterministic: fires exactly once per scene load.
    ///
    /// Usage:
    ///   - Drop this on any empty GameObject in the opening scene.
    ///   - Set <see cref="nodeId"/> to "T-01" (or any row id in text_table.tsv).
    ///   - Optional: tick <see cref="suppressIfAlreadyTriggered"/> so reload after
    ///     death doesn't replay the intro.
    /// </summary>
    [AddComponentMenu("Prism Zone/Scene Intro Player")]
    [DefaultExecutionOrder(-40)]   // after DialogueManager (-60) is ready
    public class SceneIntroPlayer : MonoBehaviour
    {
        [Header("Identity")]
        [Tooltip("Row id from text_table.tsv, e.g. T-01.")]
        [NodeIdDropdown]
        [SerializeField] private string nodeId = "T-01";

        [Header("Timing")]
        [Tooltip("Seconds to wait after scene start before firing. Lets the fade-in / " +
                 "player-spawn animations settle before the popup appears.")]
        [SerializeField] private float delaySeconds = 0.25f;

        [Tooltip("If dialogue.{nodeId}.triggered is already set (e.g. player reloaded " +
                 "after death), skip instead of replaying.")]
        [SerializeField] private bool suppressIfAlreadyTriggered = true;

        private IEnumerator Start()
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                Debug.LogWarning($"[SceneIntroPlayer:{name}] nodeId is empty.");
                yield break;
            }

            if (suppressIfAlreadyTriggered && GameFlags.Get($"dialogue.{nodeId}.triggered"))
                yield break;

            // Give DialogueManager / I18nManager / FilterManager one frame to finish Awake.
            yield return null;
            if (delaySeconds > 0f)
                yield return new WaitForSecondsRealtime(delaySeconds);

            if (DialogueManager.Instance == null)
            {
                Debug.LogWarning($"[SceneIntroPlayer:{nodeId}] No DialogueManager in scene — " +
                                 "add one to the _Bootstrap/HUD canvas.");
                yield break;
            }

            DialogueManager.Instance.ShowById(nodeId);
        }
    }
}
