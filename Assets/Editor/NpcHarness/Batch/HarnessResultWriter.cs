using System.IO;
using System.Text;
using UnityEngine;

internal static class HarnessResultWriter
{
    public static void Write(string resultPath, HarnessJobResult result)
    {
        WriteJson(resultPath, JsonUtility.ToJson(result, true), result.state, result.message);
    }

    public static void Write(string resultPath, HarnessGateResult result)
    {
        WriteJson(resultPath, JsonUtility.ToJson(result, true), result.status, result.message);
    }

    private static void WriteJson(string resultPath, string json, string state, string message)
    {
        if (string.IsNullOrWhiteSpace(resultPath))
        {
            Debug.LogError($"Harness result path is missing: {state} / {message}");
            return;
        }

        string directory = Path.GetDirectoryName(resultPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(resultPath, json, new UTF8Encoding(false));
    }
}
