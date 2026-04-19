namespace PrismZone.UI
{
    /// <summary>
    /// Five AVG popup styles defined in 功能需求文档_v1.2. Each has its own visual
    /// slot in the HUD (bottom bar, top bar, big page, fullscreen, world bubble),
    /// but they share the typewriter + queue + freeze logic in DialogueManager.
    /// </summary>
    public enum DialogueType
    {
        /// <summary>Bottom half-translucent bar; no portrait; typewriter; Esc-skippable.</summary>
        NAR,
        /// <summary>Large centered page (paper/handwritten feel); supports multi-page; re-readable; Esc-skippable.</summary>
        READ,
        /// <summary>Small top hint; auto-dismisses after a few seconds; non-intrusive.</summary>
        TIP,
        /// <summary>Fullscreen black + white text; accompanies flashback; non-skippable.</summary>
        FLASH,
        /// <summary>Small bubble anchored in world space next to an interactable object.</summary>
        ENV
    }
}
