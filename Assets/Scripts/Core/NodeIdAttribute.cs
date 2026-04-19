using UnityEngine;

namespace PrismZone.Core
{
    /// <summary>
    /// Marks a string field that holds a dialogue node id (T-XX from text_table.tsv).
    /// In Inspector renders as a dropdown listing all dialogue ids — see
    /// Editor/NodeIdDrawer.cs.
    /// </summary>
    public class NodeIdDropdownAttribute : PropertyAttribute { }

    /// <summary>
    /// Marks a string field that holds a GameFlags key. Renders as a dropdown of
    /// all known flag keys (FlagKeys.Dialogue.* + FlagKeys.Inventory.* + handwritten
    /// FlagKeysBuiltin.* via reflection).
    /// </summary>
    public class FlagKeyDropdownAttribute : PropertyAttribute { }
}
