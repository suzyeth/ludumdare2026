using System;
using UnityEngine;

namespace PrismZone.Core
{
    /// <summary>
    /// Pure data gate (FRAMEWORK.md §3.3). Used by triggers and TSV rows to ask
    /// "should I fire right now?" via <see cref="GameFlags"/>.
    ///
    /// Semantics:
    ///   - <c>requireAll</c>: every listed flag must be true (AND)
    ///   - <c>forbidAll</c>:  every listed flag must be false (NAND)
    ///   - <c>requireAny</c>: at least one listed flag must be true (OR) — only
    ///     applied when the array has entries; empty/null means "no OR clause".
    ///
    /// All three sections combine with AND. Cannot express
    /// <c>(A ∧ B) ∨ (C ∧ D)</c> — supported via custom DialogueTrigger subclass.
    /// Empty Condition (no entries) always evaluates to true.
    /// </summary>
    [Serializable]
    public struct Condition
    {
        [Tooltip("All listed flags must be true.")]
        public string[] requireAll;
        [Tooltip("All listed flags must be false.")]
        public string[] forbidAll;
        [Tooltip("At least one listed flag must be true (skipped if empty).")]
        public string[] requireAny;

        public bool Evaluate()
        {
            if (requireAll != null && requireAll.Length > 0 && !GameFlags.AllSet(requireAll)) return false;
            if (forbidAll  != null && forbidAll.Length  > 0 && !GameFlags.NoneSet(forbidAll)) return false;
            if (requireAny != null && requireAny.Length > 0 && !GameFlags.AnySet(requireAny)) return false;
            return true;
        }

        public bool IsEmpty
            => (requireAll == null || requireAll.Length == 0)
            && (forbidAll  == null || forbidAll.Length  == 0)
            && (requireAny == null || requireAny.Length == 0);
    }
}
