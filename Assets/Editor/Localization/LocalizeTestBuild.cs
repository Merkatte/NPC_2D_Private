using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public static class LocalizeTestBuild
{
    [MenuItem("Tools/Localization/Build LocalizeTest Player")]
    public static void Build()
    {
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/LocalizeTest.unity" },
            locationPathName = ".harness-runs/localization-player/LocalizeTest.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new BuildFailedException("LocalizeTest Player build failed: " + report.summary.result);
    }
}
