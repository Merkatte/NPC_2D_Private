using NUnit.Framework;
using UnityEngine;

internal sealed class HarnessValueUtilityTests
{
    [Test]
    public void ToGradientStorageColor_UsesColor32Precision()
    {
        Color requested = new Color(1f, 0.72f, 0.08f, 1f);

        Color actual = HarnessValueUtility.ToGradientStorageColor(requested);
        Color expected = (Color)new Color32(255, 184, 20, 255);

        Assert.That(HarnessValueUtility.Approximately(actual, expected), Is.True);
        Assert.That(HarnessValueUtility.Approximately(actual, requested), Is.False);
    }
}
