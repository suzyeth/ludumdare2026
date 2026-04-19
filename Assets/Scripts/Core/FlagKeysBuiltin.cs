namespace PrismZone.Core
{
    /// <summary>
    /// Hand-authored portion of FlagKeys (FRAMEWORK.md §3.2). Auto-generated
    /// dialogue + inventory entries live in the sister partial in
    /// <c>FlagKeys.cs</c> (produced by <c>FlagKeysGenerator</c>).
    ///
    /// Convention: every key is a dot-namespaced string. The first segment is
    /// the category — <see cref="Debug.FlagDebugOverlay"/> groups by it.
    /// Use these constants instead of typing strings; missing namespaces means
    /// a typo will fail to compile rather than fail at runtime.
    /// </summary>
    public static partial class FlagKeys
    {
        public static class Filter
        {
            public const string Current_None  = "filter.current.none";
            public const string Current_Red   = "filter.current.red";
            public const string Current_Green = "filter.current.green";
            public const string Unlocked      = "filter.unlocked";
        }

        public static class Broadcast
        {
            public const string PreludePlaying = "broadcast.prelude_playing";
            public const string Playing        = "broadcast.playing";
            public const string Count          = "broadcast.count";
        }

        public static class Recorder
        {
            public const string Stopped       = "recorder.stopped";
            public const string GreenPlayed   = "recorder.green_played";
        }

        public static class Scene
        {
            // Keys are derived: "scene.current." + lowercased scene name.
            // Helper to build them at runtime so callers don't typo:
            public static string Current(string sceneName) => "scene.current." + (sceneName ?? string.Empty).ToLowerInvariant();
        }

        public static class Player
        {
            public const string IsHidden    = "player.is_hidden";
            public const string BeingChased = "player.being_chased";
        }
    }
}
