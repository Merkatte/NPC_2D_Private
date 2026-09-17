using System.Runtime.InteropServices;

namespace NpcHarness;

internal static class UnityEditorLocator
{
    private const string UnityPathEnvironmentVariable = "NPC_HARNESS_UNITY_PATH";

    public static string Find(string repositoryRoot, string? explicitPath)
    {
        string? overridePath = string.IsNullOrWhiteSpace(explicitPath)
            ? Environment.GetEnvironmentVariable(UnityPathEnvironmentVariable)
            : explicitPath;
        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            string fullOverridePath = Path.GetFullPath(overridePath);
            if (!File.Exists(fullOverridePath))
            {
                throw new FileNotFoundException("The configured Unity executable does not exist.", fullOverridePath);
            }

            return fullOverridePath;
        }

        string version = ReadProjectUnityVersion(repositoryRoot);
        foreach (string candidate in GetDefaultCandidates(version))
        {
            if (File.Exists(candidate))
            {
                return Path.GetFullPath(candidate);
            }
        }

        throw new FileNotFoundException(
            $"Unity {version} was not found in a default Unity Hub location. " +
            $"Use --unity <path> or set {UnityPathEnvironmentVariable}.");
    }

    internal static string ReadProjectUnityVersion(string repositoryRoot)
    {
        string versionPath = Path.Combine(repositoryRoot, "ProjectSettings", "ProjectVersion.txt");
        string? versionLine = File.ReadLines(versionPath)
            .FirstOrDefault(line => line.StartsWith("m_EditorVersion:", StringComparison.Ordinal));
        return versionLine?.Split(':', 2)[1].Trim() ??
               throw new InvalidOperationException("ProjectSettings/ProjectVersion.txt has no Unity version.");
    }

    internal static IReadOnlyList<string> GetDefaultCandidates(string version)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return new[]
            {
                Path.Combine("/Applications", "Unity", "Hub", "Editor", version, "Unity.app", "Contents", "MacOS", "Unity"),
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Applications", "Unity", "Hub", "Editor", version, "Unity.app", "Contents", "MacOS", "Unity"),
            };
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            List<string> candidates = new List<string>();
            AddWindowsCandidate(candidates, Environment.GetEnvironmentVariable("ProgramFiles"), version);
            AddWindowsCandidate(candidates, Environment.GetEnvironmentVariable("ProgramFiles(x86)"), version);
            return candidates;
        }

        return new[]
        {
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Unity", "Hub", "Editor", version, "Editor", "Unity"),
        };
    }

    private static void AddWindowsCandidate(List<string> candidates, string? programFiles, string version)
    {
        if (!string.IsNullOrWhiteSpace(programFiles))
        {
            candidates.Add(Path.Combine(programFiles, "Unity", "Hub", "Editor", version, "Editor", "Unity.exe"));
        }
    }
}
