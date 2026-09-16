using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

internal static class HarnessJobStorage
{
    private const string HarnessLibraryDirectory = "Library/NpcHarness";

    public static string WriteInteractiveJob(HarnessJob job)
    {
        string directory = GetProjectPath(HarnessLibraryDirectory + "/Jobs");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, job.jobId + ".json");
        File.WriteAllText(path, JsonUtility.ToJson(job, true), new UTF8Encoding(false));
        return path;
    }

    public static HarnessJob LoadJob(string jobPath, out string json)
    {
        string absolutePath = Path.GetFullPath(jobPath);
        if (!File.Exists(absolutePath))
        {
            throw new FileNotFoundException("Harness Job file is missing.", absolutePath);
        }

        json = File.ReadAllText(absolutePath);
        HarnessJob job = JsonUtility.FromJson<HarnessJob>(json);
        return job ?? throw new InvalidOperationException("Harness Job JSON is empty or invalid.");
    }

    public static string GetCheckpointPath(string jobId)
    {
        string directory = GetProjectPath(HarnessLibraryDirectory + "/Checkpoints");
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, jobId + ".json");
    }

    public static void WriteCheckpoint(string checkpointPath, HarnessCheckpoint checkpoint)
    {
        File.WriteAllText(checkpointPath, JsonUtility.ToJson(checkpoint, true), new UTF8Encoding(false));
    }

    public static HarnessCheckpoint LoadCheckpoint(string checkpointPath)
    {
        string json = File.ReadAllText(checkpointPath);
        HarnessCheckpoint checkpoint = JsonUtility.FromJson<HarnessCheckpoint>(json);
        return checkpoint ?? throw new InvalidOperationException("Harness checkpoint is invalid.");
    }

    public static string ComputeHash(string json)
    {
        using SHA256 sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
        return BitConverter.ToString(hash).Replace("-", string.Empty);
    }

    public static void DeleteCheckpoint(string checkpointPath)
    {
        if (File.Exists(checkpointPath))
        {
            File.Delete(checkpointPath);
        }
    }

    private static string GetProjectPath(string relativePath)
    {
        string root = Directory.GetParent(Application.dataPath)?.FullName ??
                      throw new InvalidOperationException("Could not resolve the Unity project root.");
        return Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
    }
}
