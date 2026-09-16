using System;
using UnityEditor;
using UnityEngine;

internal static class HarnessBeaconValidator
{
    public static HarnessToolResult Validate()
    {
        try
        {
            HarnessToolContext context = new HarnessToolContext(
                new HarnessExecutionOptions(allowOverwrite: false, interactive: !Application.isBatchMode));
            GameObject beacon = context.FindSingleGameObject(HarnessBeaconRecipe.ScenePath, "/HarnessBeacon");
            Type harnessType = HarnessToolContext.ResolveAllowedComponentType("HarnessTest");
            if (beacon.GetComponents(harnessType).Length != 1)
            {
                return HarnessToolResult.Failure("HarnessBeacon must contain exactly one HarnessTest component.");
            }

            LineRenderer lineRenderer = beacon.GetComponent<LineRenderer>();
            if (!lineRenderer || lineRenderer.positionCount != 5 || !lineRenderer.sharedMaterial)
            {
                return HarnessToolResult.Failure("HarnessBeacon arrow LineRenderer is missing or incomplete.");
            }

            Material expectedMaterial = AssetDatabase.LoadAssetAtPath<Material>(HarnessBeaconRecipe.MaterialPath);
            if (!expectedMaterial || lineRenderer.sharedMaterial != expectedMaterial)
            {
                return HarnessToolResult.Failure("HarnessBeacon does not use the managed line material.");
            }

            GameObject cameraObject = context.FindSingleGameObject(HarnessBeaconRecipe.ScenePath, "/Main Camera");
            Camera camera = cameraObject.GetComponent<Camera>();
            if (!camera || !camera.orthographic || !HarnessValueUtility.Approximately(camera.orthographicSize, 2.5f))
            {
                return HarnessToolResult.Failure("Harness test scene requires the configured orthographic Main Camera.");
            }

            return HarnessToolResult.Success("HarnessBeacon scene structure is valid.");
        }
        catch (Exception exception)
        {
            return HarnessToolResult.Failure(exception.Message);
        }
    }
}
