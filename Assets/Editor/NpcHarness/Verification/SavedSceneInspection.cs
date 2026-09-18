using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class SavedSceneInspection
{
    public static HarnessGateResult Evaluate(string runId, string profile, int version, string scenePath,
        Action<Scene, HarnessGateResultBuilder> inspect)
    {
        HarnessGateResultBuilder builder = new HarnessGateResultBuilder(runId, profile, version);
        Scene preview = default;
        SortedDictionary<string, string> before = null;
        string setup = CaptureSceneSetup();
        int previewCount = EditorSceneManager.previewSceneCount;
        try
        {
            if (string.IsNullOrWhiteSpace(runId) || runId.Any(character =>
                    !(character >= 'a' && character <= 'z') && !(character >= 'A' && character <= 'Z') &&
                    !(character >= '0' && character <= '9') && character != '-' && character != '_'))
            {
                throw new InvalidOperationException("Run ID is not a safe artifact directory name.");
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                throw new InvalidOperationException("Saved scene inspection requires an idle Edit Mode editor.");
            }
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                Scene loaded = SceneManager.GetSceneAt(index);
                if (loaded.path == scenePath && loaded.isDirty)
                {
                    throw new InvalidOperationException("Target scene has unsaved changes: " + scenePath);
                }
            }
            if (!File.Exists(scenePath))
            {
                throw new InvalidOperationException("Target scene does not exist: " + scenePath);
            }
            before = CaptureDependencies(scenePath);
            RefuseDirtyDependencies(before.Keys);
            preview = EditorSceneManager.OpenPreviewScene(scenePath);
            inspect(preview, builder);
        }
        catch (Exception exception)
        {
            builder.SetInfrastructureError(exception.Message);
        }
        finally
        {
            try
            {
                if (preview.IsValid())
                {
                    EditorSceneManager.ClosePreviewScene(preview);
                }
                string afterSetup = CaptureSceneSetup();
                if (setup != afterSetup || previewCount != EditorSceneManager.previewSceneCount)
                {
                    builder.SetInfrastructureError("Inspection changed loaded scene setup or leaked a preview scene.");
                }
                else
                {
                    builder.AddPass("inspection.scene-setup", "Loaded scene setup is preserved.", setup, afterSetup);
                }
                if (before != null)
                {
                    SortedDictionary<string, string> after = CaptureDependencies(scenePath);
                    string[] changed = before.Keys.Union(after.Keys, StringComparer.Ordinal)
                        .Where(path => !before.TryGetValue(path, out string previous) ||
                            !after.TryGetValue(path, out string current) || previous != current).ToArray();
                    builder.SetChangedFiles(changed);
                    if (changed.Length > 0)
                    {
                        builder.SetInfrastructureError("Inspection changed scene dependencies: " + string.Join(", ", changed));
                    }
                    else
                    {
                        builder.AddPass("inspection.dependency-bytes", "Saved scene dependencies are byte-identical.",
                            Digest(before), Digest(after));
                    }
                    RefuseDirtyDependencies(after.Keys);
                    WriteEvidence(runId, profile, scenePath, before, after, builder);
                }
            }
            catch (Exception exception)
            {
                builder.SetInfrastructureError("Inspection cleanup/evidence failed: " + exception.Message);
            }
        }
        return builder.Build();
    }

    private static SortedDictionary<string, string> CaptureDependencies(string scenePath)
    {
        SortedDictionary<string, string> result = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (string path in AssetDatabase.GetDependencies(scenePath, recursive: true).Concat(new[] { scenePath })
                     .Distinct(StringComparer.Ordinal))
        {
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) && !path.StartsWith("Packages/", StringComparison.Ordinal))
            {
                continue;
            }
            AddFile(result, path);
            AddFile(result, path + ".meta");
        }
        return result;
    }

    private static void AddFile(IDictionary<string, string> result, string path)
    {
        if (Directory.Exists(path))
        {
            return;
        }
        if (!File.Exists(path))
        {
            result[path] = "missing";
            return;
        }
        using (SHA256 hash = SHA256.Create())
        using (FileStream stream = File.OpenRead(path))
        {
            result[path] = BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
    }

    private static void RefuseDirtyDependencies(IEnumerable<string> paths)
    {
        HashSet<string> dependencies = new HashSet<string>(paths, StringComparer.Ordinal);
        foreach (UnityEngine.Object asset in Resources.FindObjectsOfTypeAll<UnityEngine.Object>())
        {
            if (EditorUtility.IsPersistent(asset) && EditorUtility.IsDirty(asset) &&
                dependencies.Contains(AssetDatabase.GetAssetPath(asset)))
            {
                throw new InvalidOperationException("Scene dependency has unsaved in-memory changes: " + AssetDatabase.GetAssetPath(asset));
            }
        }
    }

    private static string CaptureSceneSetup()
    {
        List<string> scenes = new List<string>();
        for (int index = 0; index < SceneManager.sceneCount; index++)
        {
            Scene scene = SceneManager.GetSceneAt(index);
            scenes.Add(scene.path + "|" + scene.handle + "|loaded=" + scene.isLoaded + "|dirty=" + scene.isDirty +
                "|active=" + (scene == SceneManager.GetActiveScene()));
        }
        return string.Join(";", scenes);
    }

    private static string Digest(IEnumerable<KeyValuePair<string, string>> files)
    {
        string input = string.Join("\n", files.Select(pair => pair.Key + "=" + pair.Value));
        using (SHA256 hash = SHA256.Create())
        {
            return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(input))).Replace("-", "").ToLowerInvariant();
        }
    }

    private static void WriteEvidence(string runId, string profile, string scenePath,
        SortedDictionary<string, string> before, SortedDictionary<string, string> after, HarnessGateResultBuilder builder)
    {
        string relativePath = ".harness-runs/" + runId + "/artifacts/" + profile + "-dependencies.json";
        DependencyEvidence evidence = new DependencyEvidence
        {
            runId = runId, profile = profile, scenePath = scenePath, unityVersion = Application.unityVersion,
            beforeDigest = Digest(before), afterDigest = Digest(after),
            files = before.Keys.Union(after.Keys, StringComparer.Ordinal).OrderBy(path => path, StringComparer.Ordinal)
                .Select(path => new FileEvidence
                {
                    path = path,
                    beforeSha256 = before.TryGetValue(path, out string previous) ? previous : "absent",
                    afterSha256 = after.TryGetValue(path, out string current) ? current : "absent",
                }).ToArray(),
        };
        Directory.CreateDirectory(Path.GetDirectoryName(relativePath));
        File.WriteAllText(relativePath, JsonUtility.ToJson(evidence, prettyPrint: true));
        builder.AddArtifact("scene-dependency-sha256", relativePath);
    }

    [Serializable]
    private sealed class DependencyEvidence
    {
        public string runId, profile, scenePath, unityVersion, beforeDigest, afterDigest;
        public FileEvidence[] files;
    }

    [Serializable]
    private sealed class FileEvidence
    {
        public string path, beforeSha256, afterSha256;
    }
}
