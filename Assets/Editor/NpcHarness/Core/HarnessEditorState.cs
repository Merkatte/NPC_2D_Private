using UnityEditor;
using UnityEngine;

internal static class HarnessEditorState
{
    private const string LastResultKey = "NpcHarness.LastResult";

    public static void SetLastResult(HarnessJobResult result)
    {
        SessionState.SetString(LastResultKey, JsonUtility.ToJson(result, true));
    }

    public static HarnessJobResult GetLastResult()
    {
        string json = SessionState.GetString(LastResultKey, string.Empty);
        return string.IsNullOrWhiteSpace(json)
            ? null
            : JsonUtility.FromJson<HarnessJobResult>(json);
    }
}
