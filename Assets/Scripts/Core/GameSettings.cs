using System;
using UnityEngine;

namespace PrismZone.Core
{
    /// <summary>
    /// Global user-configurable settings, persisted to PlayerPrefs. Sliders in the
    /// Settings UI write here; systems (AudioManager, I18nManager) subscribe to
    /// <see cref="OnChanged"/> to react immediately without reloading the scene.
    ///
    /// Values are clamped on read/write — callers can pass anything.
    /// </summary>
    public static class GameSettings
    {
        // Keys kept short and namespaced to avoid collision with earlier saves.
        private const string PK_Master = "pz.master";
        private const string PK_Sfx    = "pz.sfx";
        private const string PK_Music  = "pz.music";
        private const string PK_Lang   = "pz.lang";

        public const float DefaultMaster = 1f;
        public const float DefaultSfx    = 0.8f;
        public const float DefaultMusic  = 0.55f;

        private static float _master = DefaultMaster;
        private static float _sfx    = DefaultSfx;
        private static float _music  = DefaultMusic;
        private static string _lang  = "en";
        private static bool _loaded;

        /// <summary>Fires whenever any value changes. Audio mix and UI re-read on signal.</summary>
        public static event Action OnChanged;

        public static float MasterVolume
        {
            get { EnsureLoaded(); return _master; }
            set { Set(ref _master, Mathf.Clamp01(value), PK_Master); }
        }

        public static float SfxVolume
        {
            get { EnsureLoaded(); return _sfx; }
            set { Set(ref _sfx, Mathf.Clamp01(value), PK_Sfx); }
        }

        public static float MusicVolume
        {
            get { EnsureLoaded(); return _music; }
            set { Set(ref _music, Mathf.Clamp01(value), PK_Music); }
        }

        public static string Language
        {
            get { EnsureLoaded(); return _lang; }
            set
            {
                if (string.IsNullOrEmpty(value)) return;
                EnsureLoaded();
                if (_lang == value) return;
                _lang = value;
                PlayerPrefs.SetString(PK_Lang, value);
                PlayerPrefs.Save();
                OnChanged?.Invoke();
            }
        }

        public static float EffectiveSfx   => MasterVolume * SfxVolume;
        public static float EffectiveMusic => MasterVolume * MusicVolume;

        public static void ResetToDefaults()
        {
            _master = DefaultMaster;
            _sfx    = DefaultSfx;
            _music  = DefaultMusic;
            PlayerPrefs.SetFloat(PK_Master, _master);
            PlayerPrefs.SetFloat(PK_Sfx, _sfx);
            PlayerPrefs.SetFloat(PK_Music, _music);
            PlayerPrefs.Save();
            OnChanged?.Invoke();
        }

        private static void Set(ref float field, float v, string key)
        {
            EnsureLoaded();
            if (Mathf.Approximately(field, v)) return;
            field = v;
            PlayerPrefs.SetFloat(key, v);
            PlayerPrefs.Save();
            OnChanged?.Invoke();
        }

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            _master = Mathf.Clamp01(PlayerPrefs.GetFloat(PK_Master, DefaultMaster));
            _sfx    = Mathf.Clamp01(PlayerPrefs.GetFloat(PK_Sfx,    DefaultSfx));
            _music  = Mathf.Clamp01(PlayerPrefs.GetFloat(PK_Music,  DefaultMusic));
            _lang   = PlayerPrefs.GetString(PK_Lang, "en");
        }
    }
}
