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

        [Header("Frame Montage (抽帧闪回 — PlayFrames API)")]
        [Tooltip("至少 2 张插图: [0]=A (静止+抖动), [1]=B (落地余韵)")]
        [SerializeField] private Sprite[] montageFrames;

        [Header("阶段 1: 图 A 静止")]
        [Tooltip("图 A 显示后静止多久,给观众看清画面(秒)")]
        [SerializeField] private float imageAStaticSeconds = 0.6f;

        [Header("阶段 2: 图 A 抖动爆发")]
        [Tooltip("连续抖动次数")]
        [SerializeField] private int jitterBurstCount = 3;
        [Tooltip("每次抖动间隔(秒)。0.10 ≈ 紧张节奏")]
        [SerializeField] private float jitterInterval = 0.10f;
        [Tooltip("抖动半径(UI 像素)。±X,±Y 随机")]
        [SerializeField] private float frameJitter = 4f;
        [Tooltip("抖动时每次播 FlashEcho 音效")]
        [SerializeField] private bool playEchoOnJitter = true;

        [Header("阶段 3: 黑屏 + 落地撞击")]
        [Tooltip("黑屏停顿秒数")]
        [SerializeField] private float blackScreenHoldSeconds = 0.25f;
        [Tooltip("黑屏时播放的撞击音效(重物落地)。SoundCatalog 里挂好对应 id")]
        [SerializeField] private SoundId impactSfx = SoundId.HumanBodyFall;

        [Header("阶段 4: 图 B 余韵")]
        [Tooltip("图 B 显示并静止多久(秒),最后的情感余韵")]
        [SerializeField] private float imageBStaticSeconds = 1.2f;

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

        /// <summary>
        /// 抽帧闪回 — 2+ 张插图交替闪烁,带抖动 + 加速。独立于 T-16/17 剧情流,
        /// 适合 Victory / Game Over 前的"回忆瞬间"。
        /// 调用者:在 T-21 onDialogueFinished,或 VictoryController 内 Pre-show hook。
        /// </summary>
        public void PlayFrames(Action onCompleted = null)
        {
            if (_running) { onCompleted?.Invoke(); return; }
            _onCompleted = onCompleted;
            StartCoroutine(RunFrames());
        }

        private IEnumerator RunFrames()
        {
            _running = true;

            if (montageFrames == null || montageFrames.Length < 2 || silhouette == null)
            {
                Debug.LogWarning("[Flashback] PlayFrames called but montageFrames needs 2 sprites + silhouette wired.");
                _running = false;
                _onCompleted?.Invoke();
                _onCompleted = null;
                yield break;
            }

            var rt = (RectTransform)silhouette.transform;
            Vector2 origin = rt.anchoredPosition;

            // ── t=0.00s · 黑屏淡入 ────────────────────────────────
            yield return Fade(SetOverlayAlpha, 0f, 1f, fadeInSeconds);

            // ── t=0.50s · 图 A 静止 0.6s ──────────────────────────
            silhouette.sprite = montageFrames[0];
            rt.anchoredPosition = origin;
            SetSilhouetteAlpha(1f);
            yield return new WaitForSecondsRealtime(imageAStaticSeconds);

            // ── t=1.10s · 图 A 抖动爆发 × N · 每次 FlashEcho ──────
            for (int i = 0; i < jitterBurstCount; i++)
            {
                if (frameJitter > 0f)
                {
                    rt.anchoredPosition = origin + new Vector2(
                        UnityEngine.Random.Range(-frameJitter, frameJitter),
                        UnityEngine.Random.Range(-frameJitter, frameJitter));
                }
                if (playEchoOnJitter)
                    AudioManager.Instance?.Play(echoSfx);

                yield return new WaitForSecondsRealtime(jitterInterval);
            }
            rt.anchoredPosition = origin;

            // ── t≈1.40s · 黑屏 + 落地撞击声 ───────────────────────
            SetSilhouetteAlpha(0f);
            AudioManager.Instance?.Play(impactSfx);
            yield return new WaitForSecondsRealtime(blackScreenHoldSeconds);

            // ── t≈1.65s · 图 B 静止 1.2s ──────────────────────────
            silhouette.sprite = montageFrames[1];
            SetSilhouetteAlpha(1f);
            yield return new WaitForSecondsRealtime(imageBStaticSeconds);

            // ── t≈2.85s · 淡出 ────────────────────────────────────
            SetSilhouetteAlpha(0f);
            yield return Fade(SetOverlayAlpha, 1f, 0f, fadeOutSeconds);

            _running = false;
            var cb = _onCompleted;
            _onCompleted = null;
            cb?.Invoke();
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
