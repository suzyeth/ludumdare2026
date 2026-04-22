#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using PrismZone.UI;

namespace PrismZone.DebugTools
{
    /// <summary>
    /// Dev-only tester for <see cref="FlashbackController.PlayFrames"/>.
    /// Attach to any GameObject in the starting scene (e.g. Scene_MainMenu or the
    /// first gameplay scene). On Start it waits until FlashbackController is
    /// available and fires PlayFrames once. Press <b>T</b> at any time to replay.
    ///
    /// Remove / disable this component before building release.
    /// </summary>
    public class FlashbackTester : MonoBehaviour
    {
        [Tooltip("Auto-trigger PlayFrames when the scene starts.")]
        [SerializeField] private bool autoPlayOnStart = true;

        [Tooltip("Delay before the first auto-play (gives other singletons time to Awake).")]
        [SerializeField] private float startDelay = 0.5f;

        [Tooltip("Replay key — press at any time to re-fire the flashback.")]
        [SerializeField] private Key replayKey = Key.T;

        private void Start()
        {
            if (autoPlayOnStart) StartCoroutine(AutoPlay());
        }

        private IEnumerator AutoPlay()
        {
            yield return new WaitForSecondsRealtime(startDelay);
            TryPlay();
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb[replayKey].wasPressedThisFrame) TryPlay();
        }

        private void TryPlay()
        {
            var fb = FlashbackController.Instance;
            if (fb == null)
            {
                Debug.LogWarning("[FlashbackTester] FlashbackController.Instance is null — make sure the FlashbackController prefab is in the scene (or in the persistent _Bootstrap bundle).");
                return;
            }
            Debug.Log("[FlashbackTester] PlayFrames fired.");
            fb.PlayFrames(() => Debug.Log("[FlashbackTester] PlayFrames completed."));
        }
    }
}
#endif
