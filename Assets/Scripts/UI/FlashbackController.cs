using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using PrismZone.Core;

namespace PrismZone.UI
{
    /// <summary>
    /// 4F 402 jump flashback (v1.2 T-16 → silhouette → T-17).
    ///
    /// Wiring:
    ///   - The T-15 trigger (blood blackboard READ) fires its onDialogueFinished →
    ///     FlashbackController.Instance.Play(...).
    ///   - This component owns a fullscreen black overlay (Image, alpha 0 → 1)
    ///     plus a child silhouette Image.
    ///   - Text rendering is delegated to DialogueManager FLASH popup (configured
    ///     skippable=false in inspector). We sandwich the silhouette anim between
    ///     the two FLASH calls so the player sees:
    ///         T-16 line → silhouette falling 2s → T-17 line → fade back to game.
    ///
    /// Time scale: we use unscaled time so a paused timeScale doesn't break the anim.
    /// </summary>
    public class FlashbackController : MonoBehaviour
    {
        public static FlashbackController Instance { get; private set; }

        [Header("Visuals")]
        [SerializeField] private Image blackOverlay;
        [SerializeField] private Image silhouette;
        [SerializeField] private float fadeInSeconds = 0.5f;
        [SerializeField] private float silhouetteHoldSeconds = 2f;
        [SerializeField] private float fadeOutSeconds = 0.6f;

        [Header("Silhouette Motion")]
        [Tooltip("Local Y offset (world units in canvas) where silhouette starts. Will animate to bottomY.")]
        [SerializeField] private float topY = 220f;
        [SerializeField] private float bottomY = -180f;

        [Header("Dialogue Node IDs (TSV)")]
        [SerializeField] private string nodeT16 = "T-16";
        [SerializeField] private string nodeT17 = "T-17";
        [SerializeField] private SoundId echoSfx = SoundId.FlashEcho;

        private bool _running;
        private Action _onCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            SetOverlayAlpha(0f);
            SetSilhouetteAlpha(0f);
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        public void Play(Action onCompleted = null)
        {
            if (_running) return;
            _onCompleted = onCompleted;
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            _running = true;

            // Fade overlay in so the FLASH popup arrives on top of black, not on
            // top of gameplay.
            yield return Fade(SetOverlayAlpha, 0f, 1f, fadeInSeconds);

            // T-16: scared call for father.
            yield return ShowDialogueAndWait(nodeT16);

            // Silhouette descent (placeholder — real art slides a sprite top→bottom).
            AudioManager.Instance?.Play(echoSfx);
            yield return PlaySilhouette();

            // T-17: aftermath line.
            yield return ShowDialogueAndWait(nodeT17);

            // Fade everything back.
            yield return Fade(SetOverlayAlpha, 1f, 0f, fadeOutSeconds);

            _running = false;
            var cb = _onCompleted;
            _onCompleted = null;
            cb?.Invoke();
        }

        // Wraps DialogueManager.ShowById with a hard guard: if the manager is missing
        // (e.g. boot order quirk, test scene), we MUST NOT hang in an infinite
        // `while (!finished)` — that locks the flashback on a black screen forever.
        private IEnumerator ShowDialogueAndWait(string nodeId)
        {
            var mgr = DialogueManager.Instance;
            if (mgr == null || string.IsNullOrEmpty(nodeId))
            {
                Debug.LogWarning($"[Flashback] Skipping node '{nodeId}' — DialogueManager missing.");
                yield break;
            }
            bool finished = false;
            mgr.ShowById(nodeId, () => finished = true);
            while (!finished) yield return null;
        }

        private IEnumerator PlaySilhouette()
        {
            if (silhouette == null) { yield return new WaitForSecondsRealtime(silhouetteHoldSeconds); yield break; }
            var rt = (RectTransform)silhouette.transform;
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, topY);

            float t = 0f;
            float fadeIn = 0.25f;
            // Fade in the silhouette quickly while it begins to fall.
            while (t < silhouetteHoldSeconds)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / silhouetteHoldSeconds);
                float y = Mathf.Lerp(topY, bottomY, u * u); // ease-in
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);
                float alpha = t < fadeIn ? Mathf.InverseLerp(0f, fadeIn, t) : 1f;
                SetSilhouetteAlpha(alpha);
                yield return null;
            }
            // Snap to bottom + fade out fast on impact.
            yield return Fade(SetSilhouetteAlpha, 1f, 0f, 0.2f);
        }

        private static IEnumerator Fade(Action<float> apply, float from, float to, float duration)
        {
            if (duration <= 0f) { apply(to); yield break; }
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                apply(Mathf.Lerp(from, to, Mathf.Clamp01(t / duration)));
                yield return null;
            }
            apply(to);
        }

        private void SetOverlayAlpha(float a)
        {
            if (blackOverlay == null) return;
            var c = blackOverlay.color; c.a = a; blackOverlay.color = c;
            blackOverlay.raycastTarget = a > 0.01f;
        }

        private void SetSilhouetteAlpha(float a)
        {
            if (silhouette == null) return;
            var c = silhouette.color; c.a = a; silhouette.color = c;
        }
    }
}
