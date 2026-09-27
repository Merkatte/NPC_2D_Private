using System;
using UnityEngine;

public sealed class BuildingFactory : MonoBehaviour
{
    [SerializeField] private BuildingDataContext _buildingDataContext;
    [SerializeField] private ResourceManager _resourceManager;
    [SerializeField] private DestinationDB _destinationDB;
    [SerializeField] private InteractableManager _interactableManager;
    [SerializeField] private ItemDataContext _itemDataContext;
    [SerializeField] private CropCatalog _cropCatalog;

    public bool TryCreate(BuildingDefinition definition, Transform parent, Transform entrance,
        BoxCollider2D workArea, out CompletedBuildingFacility facility, out string reason)
    {
        facility = null;
        reason = "BuildingFactory is missing a definition, prefab or scene dependency.";
        if (definition == null || !_buildingDataContext || !_resourceManager || !_destinationDB
            || !_interactableManager || !_itemDataContext || !_cropCatalog || !parent || !entrance
            || !_buildingDataContext.TryGetAssets(definition.Id, out var prefab, out _))
            return false;
        CompletedBuildingFacility created = null;
        GameObject staging = null;
        try
        {
            // Inactive parent prevents Awake/OnEnable until injection completes.
            staging = new GameObject("FacilityInitialization");
            staging.SetActive(false);
            staging.transform.SetParent(parent, false);
            created = Instantiate(prefab, staging.transform);
            created.gameObject.SetActive(false);
            if (!created.TryConfigure(definition, _resourceManager, _itemDataContext, _cropCatalog, workArea, out reason))
                return false;
            created.transform.SetParent(parent, false);
            created.transform.position += entrance.position - created.Entrance.position;
            created.gameObject.SetActive(true);
            if (!created.TryRegister(definition.BuildingType, _destinationDB, _interactableManager))
            { reason = "Facility registration failed."; return false; }
            facility = created;
            reason = null;
            return true;
        }
        catch (Exception exception) { reason = exception.Message; return false; }
        finally
        {
            if (!facility && created)
            {
                created.RemoveRegistrations();
                created.gameObject.SetActive(false);
                DestroyCreated(created.gameObject);
            }
            if (staging) DestroyCreated(staging);
        }
    }

    private static void DestroyCreated(GameObject instance)
    {
        if (Application.isPlaying) Destroy(instance);
        else DestroyImmediate(instance);
    }
}
