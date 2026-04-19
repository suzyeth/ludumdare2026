using UnityEngine;
using PrismZone.UI;

namespace PrismZone.Core
{
    /// <summary>
    /// Persistent singleton audio hub. Subscribes to gameplay events and plays
    /// the matching SoundId via the SoundCatalog. One-shot SFX pool + dedicated
    /// BGM source with crossfade.
    ///
    /// Missing clips log a warning once; gameplay is not blocked.
    /// </summary>
    [DefaultExecutionOrder(-80)]
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Catalog (loaded from Resources/Audio/SoundCatalog if unset)")]
        [SerializeField] private SoundCatalog catalog;

        // Mix values are pulled from GameSettings at play time — do not edit here.
        // Settings UI slider writes to GameSettings → OnChanged → live apply.
        private float MasterSfx   => GameSettings.EffectiveSfx;
        private float MasterMusic => GameSettings.EffectiveMusic;

        [Header("BGM fade (seconds)")]
        [SerializeField] private float bgmFade = 0.75f;

        private AudioSource[] _sfxPool;
        private const int SfxPoolSize = 6;
        private int _sfxCursor;

        private AudioSource _bgmA, _bgmB;
        private bool _bgmOnA = true;
        private AudioClip _currentBgm;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (transform.parent == null) DontDestroyOnLoad(gameObject);

            if (catalog == null) catalog = Resources.Load<SoundCatalog>("Audio/SoundCatalog");

            // SFX pool
            _sfxPool = new AudioSource[SfxPoolSize];
            for (int i = 0; i < SfxPoolSize; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f; // 2D
                _sfxPool[i] = src;
            }
            _bgmA = gameObject.AddComponent<AudioSource>();
            _bgmB = gameObject.AddComponent<AudioSource>();
            _bgmA.playOnAwake = _bgmB.playOnAwake = false;
            _bgmA.loop = _bgmB.loop = true;
            _bgmA.spatialBlend = _bgmB.spatialBlend = 0f;

            HookEvents();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            UnhookEvents();
        }

        // --- Public API ------------------------------------------------------
        public void Play(SoundId id)
        {
            if (catalog == null) return;
            var e = catalog.Get(id);
            if (e == null || e.clip == null) return;
            if (e.isMusic) { PlayBgm(e); return; }

            var src = _sfxPool[_sfxCursor];
            _sfxCursor = (_sfxCursor + 1) % _sfxPool.Length;
            src.clip = e.clip;
            src.volume = e.volume * MasterSfx;
            src.pitch = e.pitch;
            src.loop = e.loop;
            src.Play();
        }

        public void StopBgm() => StartCoroutine(CrossFade(null));

        // --- BGM crossfade ---------------------------------------------------
        private void PlayBgm(SoundCatalog.Entry e)
        {
            if (e.clip == _currentBgm) return;
            _currentBgm = e.clip;
            StartCoroutine(CrossFade(e));
        }

        private System.Collections.IEnumerator CrossFade(SoundCatalog.Entry e)
        {
            var from = _bgmOnA ? _bgmA : _bgmB;
            var to   = _bgmOnA ? _bgmB : _bgmA;
            _bgmOnA = !_bgmOnA;

            if (e != null && e.clip != null)
            {
                to.clip = e.clip;
                to.pitch = e.pitch;
                to.volume = 0f;
                to.Play();
            }

            float t = 0f;
            float fromStart = from.volume;
            float toTarget = e != null ? e.volume * MasterMusic : 0f;
            while (t < bgmFade)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / bgmFade);
                from.volume = Mathf.Lerp(fromStart, 0f, k);
                to.volume = Mathf.Lerp(0f, toTarget, k);
                yield return null;
            }
            from.Stop();
        }

        // --- Event hooks -----------------------------------------------------
        private void HookEvents()
        {
            FilterManager.OnFilterChanged += HandleFilter;
            GameOverController.OnGameOver += HandleGameOver;
            VictoryController.OnVictory += HandleVictory;
            PrismZone.Interact.PasscodeDoor.OnRequestPasscode += HandlePasscodePrompt;
            GameSettings.OnChanged += HandleSettingsChanged;
        }

        private void UnhookEvents()
        {
            FilterManager.OnFilterChanged -= HandleFilter;
            GameOverController.OnGameOver -= HandleGameOver;
            VictoryController.OnVictory -= HandleVictory;
            PrismZone.Interact.PasscodeDoor.OnRequestPasscode -= HandlePasscodePrompt;
            GameSettings.OnChanged -= HandleSettingsChanged;
        }

        // BGM already-playing source must retune when sliders move; one-shot SFX
        // read MasterSfx at Play() time so they're automatically correct.
        private void HandleSettingsChanged()
        {
            var active = _bgmOnA ? _bgmA : _bgmB;
            if (active != null && active.isPlaying && _currentBgm != null)
            {
                var e = catalog != null ? catalog.Get(FindIdForClip(_currentBgm)) : null;
                float baseVol = e != null ? e.volume : 1f;
                active.volume = baseVol * MasterMusic;
            }
        }

        private SoundId FindIdForClip(AudioClip clip)
        {
            // Rare path — only on settings change. Reverse lookup avoids tracking
            // the current SoundId separately.
            if (catalog == null || clip == null) return SoundId.None;
            foreach (SoundId id in System.Enum.GetValues(typeof(SoundId)))
            {
                var e = catalog.Get(id);
                if (e != null && e.clip == clip) return id;
            }
            return SoundId.None;
        }

        private void HandleFilter(FilterColor prev, FilterColor next)
        {
            Play(next == FilterColor.None ? SoundId.FilterOff : SoundId.FilterSwap);
        }

        private void HandleGameOver(string reason) => Play(SoundId.GameOver);
        private void HandleVictory(string reason) => Play(SoundId.Victory);
        private void HandlePasscodePrompt(PrismZone.Interact.PasscodeDoor d) => Play(SoundId.PopupOpen);
    }
}
