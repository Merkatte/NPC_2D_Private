using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class LocalizeEditorTests
{
    [MenuItem("Tools/Localization/Run Edit Mode Checks")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Run localization Editor checks outside Play Mode.");
        int count = LocalizeTests.RunPure();
        if (!LocalizeKeyGenerator.TryValidateBuild(out string error))
            throw new InvalidOperationException(error);
        LocalizeData data = ScriptableObject.CreateInstance<LocalizeData>();
        TextAsset csv = new TextAsset(LocalizeTests.CreateCompiledEnumFixture());
        TextAsset changed = new TextAsset(csv.text.Replace(",fixture", ",edited"));
        try
        {
            FieldInfo csvField = typeof(LocalizeData).GetField("_csv", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo cacheField = typeof(LocalizeData).GetField("_texts", BindingFlags.Instance | BindingFlags.NonPublic);
            csvField.SetValue(data, csv);
            Require(data.TryGetText(LocalizeKey.UI_Confirm, "ko", out string text) && text == "fixture", "Korean column lookup");
            object cache = cacheField.GetValue(data);
            Require(data.TryGetText(LocalizeKey.UI_Confirm, "ko", out _) && ReferenceEquals(cache, cacheField.GetValue(data)), "Cache reuse");
            Require(!data.TryGetText((LocalizeKey)int.MaxValue, "ko", out text) && text == string.Empty, "Missing key");
            Require(!data.TryGetText(LocalizeKey.UI_Confirm, "en", out text) && text == string.Empty, "Missing language");
            csvField.SetValue(data, changed);
            data.InvalidateCache();
            Require(data.TryGetText(LocalizeKey.UI_Confirm, "ko", out text) && text == "edited", "Cache invalidation");
            // Simulate a new play session without altering project Play Mode options.
            csvField.SetValue(data, csv);
            typeof(LocalizeData).GetMethod("BeginPlaySession", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
            Require(data.TryGetText(LocalizeKey.UI_Confirm, "ko", out text) && text == "fixture", "New session cache");
            Debug.Log("Localize checks PASS: " + count + " pure cases, build consistency, 6 Editor cache/query cases. Play/Player not covered.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(data);
            UnityEngine.Object.DestroyImmediate(csv);
            UnityEngine.Object.DestroyImmediate(changed);
        }
    }

    private static void Require(bool success, string message)
    {
        if (!success)
            throw new InvalidOperationException(message);
    }
}
