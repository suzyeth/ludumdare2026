using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrismZone.Core
{
    /// <summary>
    /// Minimal flat-JSON i18n loader. Expects Assets/Resources/i18n/{lang}.json with
    /// a single-level { "key": "value" } object. Missing keys fall back to the key
    /// string so UI never shows blank.
    /// </summary>
    public static class I18nManager
    {
        private const string PrefKey = "lang";
        // Default to English for MVP smoke test — TMP's default LiberationSans SDF
        // lacks CJK glyphs. Switch to "zh" after wiring a Chinese-capable font (Zpix).
        private const string DefaultLang = "en";
        private const string ResourcePath = "i18n/";

        private static Dictionary<string, string> _map = new Dictionary<string, string>();
        private static string _lang = DefaultLang;
        private static bool _initialised;

        public static string CurrentLang => _lang;
        public static event Action OnLanguageChanged;

        public static void Init()
        {
            var saved = PlayerPrefs.GetString(PrefKey, DefaultLang);
            Load(saved);
            _initialised = true;
        }

        private static void EnsureInit()
        {
            if (_initialised) return;
            Init();
        }

        public static void SetLanguage(string lang)
        {
            if (lang == _lang) return;
            Load(lang);
            PlayerPrefs.SetString(PrefKey, lang);
            OnLanguageChanged?.Invoke();
        }

        public static string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            EnsureInit();
            return _map.TryGetValue(key, out var v) ? v : key;
        }

        public static string Format(string key, params object[] args)
        {
            var raw = Get(key);
            try { return string.Format(raw, args); }
            catch { return raw; }
        }

        private static void Load(string lang)
        {
            _lang = lang;
            _map = new Dictionary<string, string>();
            var asset = Resources.Load<TextAsset>(ResourcePath + lang);
            if (asset == null)
            {
                Debug.LogWarning($"[I18n] Missing locale file: {ResourcePath}{lang}.json — keys will pass through.");
                return;
            }
            ParseFlatJson(asset.text, _map);
        }

        private static void ParseFlatJson(string json, Dictionary<string, string> target)
        {
            var wrapper = JsonUtility.FromJson<JsonKvWrapper>(WrapAsArray(json));
            if (wrapper?.items == null) return;
            foreach (var kv in wrapper.items)
            {
                if (!string.IsNullOrEmpty(kv.k)) target[kv.k] = kv.v ?? string.Empty;
            }
        }

        private static string WrapAsArray(string flatJson)
        {
            // JsonUtility can't read arbitrary dicts. Transform {"a":"b","c":"d"}
            // into {"items":[{"k":"a","v":"b"},{"k":"c","v":"d"}]} once at load.
            var sb = new System.Text.StringBuilder(flatJson.Length + 32);
            sb.Append("{\"items\":[");
            int i = 0;
            bool first = true;
            while (i < flatJson.Length)
            {
                if (flatJson[i] != '"') { i++; continue; }
                int keyStart = ++i;
                while (i < flatJson.Length && flatJson[i] != '"') { if (flatJson[i] == '\\') i++; i++; }
                string key = flatJson.Substring(keyStart, i - keyStart);
                i++;
                while (i < flatJson.Length && flatJson[i] != ':') i++;
                i++;
                while (i < flatJson.Length && char.IsWhiteSpace(flatJson[i])) i++;
                if (i >= flatJson.Length || flatJson[i] != '"') continue;
                int valStart = ++i;
                while (i < flatJson.Length && flatJson[i] != '"') { if (flatJson[i] == '\\') i++; i++; }
                string val = flatJson.Substring(valStart, i - valStart);
                i++;
                if (!first) sb.Append(',');
                sb.Append("{\"k\":\"").Append(key).Append("\",\"v\":\"").Append(val).Append("\"}");
                first = false;
            }
            sb.Append("]}");
            return sb.ToString();
        }

        [Serializable] private class JsonKv { public string k; public string v; }
        [Serializable] private class JsonKvWrapper { public JsonKv[] items; }
    }
}
