using System;
using UnityEngine;

namespace PrismZone.Core
{
    /// <summary>
    /// Single row of <c>text_table.tsv</c> after import (FRAMEWORK.md §3.5).
    /// Held in <see cref="TextTable.entries"/>; the SO indexes by id at load.
    ///
    /// Multi-page text uses a literal <c>|</c> (pipe) separator inside zh/en cells:
    ///   "page one text|page two text|page three text"
    /// — see <see cref="TextTable.PageCount"/> and the <c>page</c> param of
    /// <see cref="TextTable.T"/>.
    ///
    /// Filter-conditional content lives in the <c>zh_red/en_red/zh_green/en_green</c>
    /// columns. When a column for the current filter is non-empty it overrides
    /// the base zh/en column.
    /// </summary>
    [Serializable]
    public class TextEntry
    {
        public enum Category { dialogue, item, ui, prompt, system }
        public enum AvgType  { None, NAR, READ, TIP, FLASH, ENV }
        public enum FilterReq { None, Red, Green, Any }
        public enum TriggerMode { Manual, OnEnter, OnPickup, OnInteract, OnState }

        public string id;
        public Category category;
        public AvgType avgType;

        // Base text (always required) + 4 filter-conditional overrides.
        public string zh;
        public string en;
        public string zh_red;
        public string en_red;
        public string zh_green;
        public string en_green;

        public FilterReq filterReq;
        public bool repeatable;
        public bool cantSkip;
        public float autoHideSec;
        public string followUpId;
        public TriggerMode triggerMode;

        // Comma-separated SoundId names; AudioManager resolves each in order.
        public string sfx;
        public string notes;

        // Conditional columns (FRAMEWORK §3.5 patch #3) — comma-separated flag keys.
        public string requireFlagsRaw;
        public string forbidFlagsRaw;
        public string setFlagsOnFireRaw;

        // Pre-split arrays cached by importer for O(1) Condition evaluation.
        // Not authored — TextTableImporter populates these from the *Raw fields.
        [HideInInspector] public string[] requireFlags;
        [HideInInspector] public string[] forbidFlags;
        [HideInInspector] public string[] setFlagsOnFire;
    }
}
