using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public sealed class LocalizeBuildValidator : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (!LocalizeKeyGenerator.TryValidateBuild(out string error))
            throw new BuildFailedException(error);
    }
}
