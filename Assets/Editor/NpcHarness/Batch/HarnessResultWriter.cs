using System.IO;
using System.Text;
using UnityEngine;

internal static class HarnessResultWriter
{
    public static void Write(string resultPath, HarnessJobResult result)
    {
        if (string.IsNullOrWhiteSpace(resultPath))
        {
            Debug.LogError($"Harness result path is missing: {result.state} / {result.message}");
            return;
        }

        string directory = Path.GetDirectoryName(resultPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(resultPath, JsonUtility.ToJson(result, true), new UTF8Encoding(false));
    }
}
