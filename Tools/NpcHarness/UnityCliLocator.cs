namespace NpcHarness;

internal static class UnityCliLocator
{
    public static string Find(string? explicitPath)
    {
        string? configured = explicitPath ?? Environment.GetEnvironmentVariable("NPC_HARNESS_UNITY_CLI_PATH");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            string fullPath = Path.GetFullPath(configured);
            return File.Exists(fullPath) ? fullPath : throw new FileNotFoundException("Unity CLI override does not exist.", fullPath);
        }

        string executable = OperatingSystem.IsWindows() ? "unity.exe" : "unity";
        IEnumerable<string> pathCandidates = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(directory => Path.Combine(directory.Trim('"'), executable));
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string[] defaults = OperatingSystem.IsWindows()
            ? new[] { Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Unity", "bin", executable),
                Path.Combine(home, ".unity", "bin", executable) }
            : new[] { Path.Combine(home, ".local", "bin", executable), Path.Combine(home, ".unity", "bin", executable),
                "/opt/homebrew/bin/unity", "/usr/local/bin/unity", "/usr/bin/unity" };
        foreach (string candidate in pathCandidates.Concat(defaults))
            if (File.Exists(candidate))
                return Path.GetFullPath(candidate);
        throw new FileNotFoundException("Official Unity CLI was not found. Set --unity-cli or NPC_HARNESS_UNITY_CLI_PATH.");
    }
}
