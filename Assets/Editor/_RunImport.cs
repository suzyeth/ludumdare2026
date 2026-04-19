using UnityEditor;
using PrismZone.EditorTools;

public static class _RunImport
{
    public static void Execute()
    {
        TextTableImporter.ImportNow(verbose: true);
    }
}
