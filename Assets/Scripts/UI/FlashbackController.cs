using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using PrismZone.Core;

namespace PrismZone.UI
{
    /// <summary>
    /// 抽帧闪回 — full-screen montage of 2+ illustrations with a jitter burst
    /// and impact SFX. Single entry point: <see cref="PlayFrames"/>.
    ///
    /// Visual stack:
    ///   - <see cref="blackOverlay"/>  fades 0 → 1 to cover gameplay
    ///   - <see cref="montageImage"/>  anchored stretch-both, renders the montage sprites
    ///
    /// All timing uses unscaled time so a paused game doesn't freeze the anim.
    /// </summary>
    public class FlashbackController : MonoBehaviour
    {
        public static FlashbackController Instance { get; private set; }

        [Header("Visuals")]
        [SerializeField] private Image blackOverlay;
        [Tooltip("Full-screen image that renders the montage sprites. Its RectTransform should be stretch-both with all insets at 0.")]
        [FormerlySerializedAs("silhouette")]
        [SerializeField] private Image montageImage;
        [SerializeField] private float fadeInSeconds = 0.5f;
        [SerializeField] private float fadeOutSeconds = 0.6f;

        [SerializeField] private SoundId echoSfx = SoundId.FlashEcho;

        [Header("Frame Montage")]
        [Tooltip("至少 2 张全屏插图: [0]=A (静止+抖动), [1]=B (落地余韵)")]
        [SerializeField] private Sprite[] montageFrames;

        [Header("阶段 1: 图 A 静止")]
        [Tooltip("图 A 显示后静止多久,给观众看清画面(秒)")]
        [SerializeField] private float imageAStaticSeconds = 0.6f;

        [Header("阶段 2: 图 A 抖动爆发")]
        [Tooltip("连续抖动次数")]
        [SerializeField] private int jitterBurstCount = 8;
        [Tooltip("每次抖动间隔(秒)。0.06–0.08 ≈ strobe 闪烁感")]
        [SerializeField] private float jitterInterval = 0.07f;
        [Tooltip("抖动半径(UI 像素)。±X,±Y 随机")]
        [SerializeField] private float frameJitter = 12f;
        [Tooltip("抖动时每次播 FlashEcho 音效")]
        [SerializeField] private bool playEchoOnJitter = true;
        [Tooltip("抖动时在 '图 A 可见' 与 '全黑' 之间 strobe 切换,模拟老录像带撕裂。关=仅位移抖动。")]
        [SerializeField] private bool strobeAlphaFlicker = true;

        [Header("阶段 3: 静默黑屏 → 落地撞击 → 图 B")]
        [Tooltip("黑屏静默停顿秒数 (无音效)")]
        [SerializeField] private float blackScreenHoldSeconds = 0.5f;
        [Tooltip("落地撞击音效。SoundCatalog 里挂好对应 id")]
        [SerializeField] private SoundId impactSfx = SoundId.HumanBodyFall;
        [Tooltip("撞击音效播放后到图 B 出现之间的延迟 (大致 = 音效时长)。0.5–1.0s 常用。")]
        [SerializeField] private float impactSfxLeadSeconds = 0.7f;

        [Header("阶段 4: 图 B 余韵")]
        [Tooltip("图 B 显示并静止多久(秒),最后的情感余韵")]
        [SerializeField] private float imageBStaticSeconds = 1.2f;

        private bool _running;
        private Action _onCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            // Montage image uses white tint so the sprite's own colors render
            // untouched — RGB-black would multiply the sprite to all-black even
            // at alpha=1. Only alpha is tweened at runtime.
            if (montageImage != null) montageImage.color = new Color(1f, 1f, 1f, 0f);
            SetOverlayAlpha(0f);
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>
        /// 抽帧闪回 — 2+ 张全屏插图交替闪烁,带抖动 + 落地撞击。
        /// 调用者: 在 T-21 onDialogueFinished,或 VictoryController 内 Pre-show hook。
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

            if (montageFrames == null || montageFrames.Length < 2 || montageImage == null)
            {
                Debug.LogWarning("[Flashback] PlayFrames called but montageFrames needs 2 sprites + montageImage wired.");
                _running = false;
                _onCompleted?.Invoke();
                _onCompleted = null;
                yield break;
            }

            var rt = (RectTransform)montageImage.transform;
            Vector2 origin = rt.anchoredPosition;

            // ── t=0.00s · 黑屏淡入 ────────────────────────────────
            yield return Fade(SetOverlayAlpha, 0f, 1f, fadeInSeconds);

            // ── t=0.50s · 图 A 静止 0.6s ──────────────────────────
            montageImage.sprite = montageFrames[0];
            rt.anchoredPosition = origin;
            SetMontageAlpha(1f);
            yield return new WaitForSecondsRealtime(imageAStaticSeconds);

            // ── 阶段 2 · 图 A 抖动爆发 (strobe 闪烁) ─────────────
            // Position jitter + alpha strobe give the VHS-tear / flashbulb
            // vibe. Alternate ticks kill the image to full black so the A→void
            // contrast reads as flicker rather than just shake.
            for (int i = 0; i < jitterBurstCount; i++)
            {
                if (frameJitter > 0f)
                {
                    rt.anchoredPosition = origin + new Vector2(
                        UnityEngine.Random.Range(-frameJitter, frameJitter),
                        UnityEngine.Random.Range(-frameJitter, frameJitter));
                }
                if (strobeAlphaFlicker)
                {
                    SetMontageAlpha((i & 1) == 0 ? UnityEngine.Random.Range(0.7f, 1f) : 0f);
                }
                if (playEchoOnJitter)
                    AudioManager.Instance?.Play(echoSfx);

                yield return new WaitForSecondsRealtime(jitterInterval);
            }
            rt.anchoredPosition = origin;

            // ── 阶段 3a · 静默黑屏 (no sfx) ────────────────────────
            SetMontageAlpha(0f);
            yield return new WaitForSecondsRealtime(blackScreenHoldSeconds);

            // ── 阶段 3b · 落地撞击声,等音效放完 ────────────────────
            AudioManager.Instance?.Play(impactSfx);
            yield return new WaitForSecondsRealtime(impactSfxLeadSeconds);

            // ── 阶段 4 · 图 B 静止 ────────────────────────────────
            montageImage.sprite = montageFrames[1];
            SetMontageAlpha(1f);
            yield return new WaitForSecondsRealtime(imageBStaticSeconds);

            // ── t≈2.85s · 淡出 ────────────────────────────────────
            SetMontageAlpha(0f);
            yield return Fade(SetOverlayAlpha, 1f, 0f, fadeOutSeconds);

            _running = false;
            var cb = _onCompleted;
            _onCompleted = null;
            cb?.Invoke();
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

        private void SetMontageAlpha(float a)
        {
            if (montageImage == null) return;
            // Force white tint so the sprite's own colors come through even if
            // someone authored a black tint in the prefab.
            montageImage.color = new Color(1f, 1f, 1f, a);
        }
    }
}
