using System;
using UnityEngine;

namespace PrismZone.Core
{
    /// <summary>
    /// Thin wrapper around <see cref="TextTable"/> kept for back-compat with the
    /// many UI components that still call <c>I18nManager.Get</c> / <c>Format</c>.
    /// All actual data lives in <c>Resources/TextTable.asset</c> (built from
    /// <c>Assets/Config/text_table.tsv</c> by the importer).
    ///
    /// Language switching:
    ///   - <see cref="SetLanguage"/> updates <see cref="CurrentLang"/> + fires the
    ///     <see cref="OnLanguageChanged"/> event so HUDs can re-read.
    ///   - Default is "en"; players switch via the SettingsPanel language button.
    /// </summary>
    public static class I18nManager
    {
        private const string PrefKey = "lang";
        private const string DefaultLang = "en";

        public static string CurrentLang { get; private set; } = DefaultLang;
        public static event Action OnLanguageChanged;

        public static void Init()
        {
            CurrentLang = PlayerPrefs.GetString(PrefKey, DefaultLang);
        }

        public static void ResetPrefs()
        {
            PlayerPrefs.DeleteKey(PrefKey);
            PlayerPrefs.Save();
        }

        public static void SetLanguage(string lang)
        {
            if (string.IsNullOrEmpty(lang) || lang == CurrentLang) return;
            CurrentLang = lang;
            PlayerPrefs.SetString(PrefKey, lang);
            OnLanguageChanged?.Invoke();
        }

        /// <summary>
        /// Resolve an id to display text. For dialogue nodes pass the T-XX id;
        /// the table chooses the right column for current language and active
        /// filter. Missing ids return the id itself so designers see the gap.
        /// </summary>
        public static string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            var filter = FilterManager.Instance != null ? FilterManager.Instance.Current : FilterColor.None;
            return TextTable.T(key, 0, CurrentLang, filter);
        }

        public static string Format(string key, params object[] args)
        {
            var raw = Get(key);
            try { return string.Format(raw, args); }
            catch { return raw; }
        }
    }
}
