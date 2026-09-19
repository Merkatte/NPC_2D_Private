using System;
using UnityEditor;

public sealed class LocalizeCsvPostprocessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
        string[] movedAssets, string[] movedFromAssetPaths)
    {
        if (!ContainsCsv(importedAssets) && !ContainsCsv(deletedAssets) &&
            !ContainsCsv(movedAssets) && !ContainsCsv(movedFromAssetPaths))
            return;

        // Coalesce callbacks and leave the current import transaction before importing generated C#.
        EditorApplication.delayCall -= ProcessChange;
        EditorApplication.delayCall += ProcessChange;
    }

    private static bool ContainsCsv(string[] paths) =>
        Array.IndexOf(paths, LocalizeKeyGenerator.CsvPath) >= 0;

    private static void ProcessChange()
    {
        LocalizeKeyGenerator.Regenerate();
        foreach (string guid in AssetDatabase.FindAssets("t:LocalizeData"))
        {
            LocalizeData data = AssetDatabase.LoadAssetAtPath<LocalizeData>(AssetDatabase.GUIDToAssetPath(guid));
            if (data)
                data.InvalidateCache();
        }
    }
}
