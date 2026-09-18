using System.Security.Cryptography;
using System.Text.Json;
using static NpcHarness.ReviewEvidenceJson;

namespace NpcHarness;

internal static class ReviewEvidenceInputs
{
    public static string Sha256(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    public static string Resolve(string root, string path)
    {
        Require(!string.IsNullOrWhiteSpace(path) && !Path.IsPathRooted(path) && !path.Contains('\\') &&
                !path.Contains(':') && !path.Any(char.IsControl), "Expected normalized repository-relative path.");
        string[] parts = path.Split('/');
        Require(parts.All(p => p.Length > 0 && p != "." && p != ".." && p == p.Trim() &&
                !p.EndsWith('.') && p.IndexOfAny(Path.GetInvalidFileNameChars()) < 0), "Unsafe path: " + path);
        Require(!parts.Any(p => p.Equals(".claude", StringComparison.OrdinalIgnoreCase) ||
                               p.Equals("CLAUDE.md", StringComparison.OrdinalIgnoreCase) ||
                               p.Equals(".git", StringComparison.OrdinalIgnoreCase)), "Excluded path: " + path);
        string full = Path.GetFullPath(root);
        Require(!new DirectoryInfo(full).Attributes.HasFlag(FileAttributes.ReparsePoint), "Linked repository root.");
        foreach (string part in parts)
        {
            full = Path.Combine(full, part);
            // File.GetAttributes also sees dangling links; a missing ordinary path is allowed.
            try
            {
                Require(!File.GetAttributes(full).HasFlag(FileAttributes.ReparsePoint), "Linked path: " + path);
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
        }
        return full;
    }

    public static SortedDictionary<string, string> Capture(string root, JsonElement request)
    {
        SortedDictionary<string, string> files = new(StringComparer.Ordinal);
        foreach (string input in Strings(request, "inputRoots"))
        {
            Require(!input.Split('/').Any(IsExcluded), "Excluded input root: " + input);
            string full = Resolve(root, input);
            Require(File.Exists(full) || Directory.Exists(full), "Missing input root: " + input);
            Visit(input);
        }
        Require(files.Count > 0, "No candidate input files.");
        Unique(files.Keys, "snapshot paths");
        foreach (JsonElement rule in Rows(request, "rules"))
            Require(files.ContainsKey(Text(rule, "documentPath")), "Rule document is not captured by inputRoots.");
        return files;

        void Visit(string relative)
        {
            string full = Resolve(root, relative);
            if (Directory.Exists(full))
            {
                foreach (string entry in Directory.EnumerateFileSystemEntries(full).Order(StringComparer.Ordinal))
                {
                    string name = Path.GetFileName(entry);
                    if (!IsExcluded(name)) Visit(relative + "/" + name);
                }
            }
            else
            {
                Require(File.Exists(full), "Missing input: " + relative);
                files[relative] = Sha256(full);
            }
        }
    }

    public static bool EqualFiles(JsonElement saved, SortedDictionary<string, string> actual)
    {
        Require(saved.ValueKind == JsonValueKind.Object, "Invalid snapshot files.");
        Dictionary<string, string> expected = new(StringComparer.Ordinal);
        foreach (JsonProperty file in saved.EnumerateObject())
        {
            Require(file.Value.ValueKind == JsonValueKind.String, "Invalid file digest.");
            string hash = file.Value.GetString()!;
            Hash(hash);
            expected.Add(file.Name, hash);
        }
        return expected.Count == actual.Count && expected.All(p => actual.TryGetValue(p.Key, out string? hash) &&
            string.Equals(p.Value, hash, StringComparison.OrdinalIgnoreCase));
    }

    public static void WriteNew(string path, object value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using FileStream stream = new(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        JsonSerializer.Serialize(stream, value, new JsonSerializerOptions { WriteIndented = true });
    }

    private static bool IsExcluded(string name) => new[]
    {
        ".git", ".claude", "CLAUDE.md", ".harness-runs", "bin", "obj",
        "Library", "Temp", "Logs", "Build", "Builds", "UserSettings", "_Recovery", "agent-runs",
    }.Contains(name, StringComparer.OrdinalIgnoreCase);
}
