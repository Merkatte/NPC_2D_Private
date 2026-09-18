using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public static class FarmerSceneAcceptanceRunner
{
    public static void RunFromCommandLine()
    {
        string resultPath = HarnessBatchRunner.GetRequiredArgument("-harnessResultPath");
        string runId = HarnessBatchRunner.GetOptionalArgument("-harnessRunId", "farmer-structure-self-test");
        HarnessGateResultBuilder builder = new HarnessGateResultBuilder(runId, "Harness.FarmerStructure.SelfTest", 1);
        Type[] fixtureTypes =
        {
            typeof(FarmerSceneStructureGateTests),
            typeof(DeclarativeSceneGateManifestTests),
            typeof(HarnessBeaconPlayModeGateTests),
            typeof(HarnessBeaconStructureGateTests),
            typeof(HarnessGateResultBuilderTests),
            typeof(HarnessInteractiveGateRequestPolicyTests),
            typeof(HarnessValueUtilityTests),
            typeof(SquareCharacterStructureGateTests),
        };
        foreach (Type fixtureType in fixtureTypes)
        {
            MethodInfo[] methods = fixtureType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .OrderBy(method => method.Name, StringComparer.Ordinal).ToArray();
            // These bounded fixtures use only synchronous Test/TestCase methods. Fail closed
            // if fixture lifecycle hooks are introduced instead of silently skipping them.
            MethodInfo[] allMethods = fixtureType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (allMethods.Any(method => method.IsDefined(typeof(SetUpAttribute), true)
                || method.IsDefined(typeof(TearDownAttribute), true)
                || method.IsDefined(typeof(OneTimeSetUpAttribute), true)
                || method.IsDefined(typeof(OneTimeTearDownAttribute), true)))
            {
                builder.AddFailure("self-test." + fixtureType.Name + ".lifecycle",
                    "Fixture lifecycle requires the NUnit runner.", "No fixture lifecycle hooks", "Unsupported lifecycle hooks");
                continue;
            }
            foreach (MethodInfo test in methods)
            {
                TestCaseAttribute[] cases = test.GetCustomAttributes(typeof(TestCaseAttribute), false).Cast<TestCaseAttribute>().ToArray();
                if (cases.Length > 0)
                {
                    for (int index = 0; index < cases.Length; index++)
                        RunTest(builder, fixtureType, test, cases[index].Arguments, ".case-" + index);
                }
                else if (test.IsDefined(typeof(TestAttribute), false))
                    RunTest(builder, fixtureType, test, Array.Empty<object>(), string.Empty);
            }
        }

        HarnessGateResult result = builder.Build();
        HarnessResultWriter.Write(resultPath, result);
        Debug.Log("Farmer scene independent acceptance: " + result.message);
        if (Application.isBatchMode)
            EditorApplication.Exit(result.success ? 0 : 1);
    }

    private static void RunTest(HarnessGateResultBuilder builder, Type fixtureType, MethodInfo test, object[] arguments, string suffix)
    {
        string checkId = "self-test." + fixtureType.Name + "." + test.Name + suffix;
        try
        {
            Assert.That(test.ReturnType, Is.EqualTo(typeof(void)), "Only synchronous tests are supported.");
            test.Invoke(Activator.CreateInstance(fixtureType), arguments);
            builder.AddPass(checkId, "Independent assertion passed.", "All assertions pass", "All assertions passed");
        }
        catch (Exception exception)
        {
            Exception cause = exception is TargetInvocationException invocation && invocation.InnerException != null
                ? invocation.InnerException : exception;
            builder.AddFailure(checkId, cause.Message, "All assertions pass", cause.ToString());
        }
    }
}
