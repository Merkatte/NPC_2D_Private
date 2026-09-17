namespace NpcHarness;

internal static class RepositoryLocator
{
    public static string FindRoot(string startDirectory)
    {
        DirectoryInfo? directory = new DirectoryInfo(startDirectory);
        while (directory != null)
        {
            string projectVersionPath = Path.Combine(directory.FullName, "ProjectSettings", "ProjectVersion.txt");
            string assetsPath = Path.Combine(directory.FullName, "Assets");
            if (File.Exists(projectVersionPath) && Directory.Exists(assetsPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Run the harness from inside the Unity project repository.");
    }
}
