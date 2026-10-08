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
    [SerializeField] private HousingDataContext _housingDataContext;
    [SerializeField] private HousingManager _housingManager;

    public bool CanStartNewConstruction(BuildingDefinition definition)
        => definition != null && (definition.BuildingType != BuildingType.House
            || (_housingDataContext && _housingManager
                && _housingDataContext.TryGetTierByBuildingId(definition.Id, out var tier) && tier.Tier == 1));

    public bool TryGetNextUpgrade(CompletedBuildingFacility facility, out BuildingDefinition definition)
    {
        definition = null;
        return facility && facility.House && facility.House.Tier != null && _housingDataContext
            && _buildingDataContext
            && _housingDataContext.TryGetNextTier(facility.House.Tier.Tier, out var next)
            && _buildingDataContext.TryGetBuildingDefinition(next.BuildingId, out definition);
    }

    public bool TryUpgrade(CompletedBuildingFacility facility, BuildingDefinition definition, out string reason)
    {
        reason = "주택 업그레이드 정의 또는 이미지가 올바르지 않습니다.";
        if (!facility || !facility.House || definition == null || !_housingDataContext
            || !TryGetNextUpgrade(facility, out var next) || next.Id != definition.Id
            || !_housingDataContext.TryGetTierByBuildingId(definition.Id, out var tier)
            || !_buildingDataContext.TryGetAssets(definition.Id, out _, out var sprite))
            return false;
        return facility.TryApplyHouseTier(tier, sprite, out reason);
    }

    public bool TryCreate(BuildingDefinition definition, Transform parent, Transform entrance,
        BoxCollider2D workArea, out CompletedBuildingFacility facility, out string reason)
    {
        facility = null;
        reason = "BuildingFactory is missing a definition, prefab or scene dependency.";
        if (definition == null || !_buildingDataContext || !_resourceManager || !_destinationDB
            || !_interactableManager || !_itemDataContext || !_cropCatalog || !parent || !entrance
            || !CanStartNewConstruction(definition)
            || !_buildingDataContext.TryGetAssets(definition.Id, out var prefab, out var icon))
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
            if (definition.BuildingType == BuildingType.House
                && (!_housingDataContext.TryGetTierByBuildingId(definition.Id, out var tier)
                    || !created.TryConfigureHouse(_housingDataContext, tier, _housingManager, icon, out reason)))
                return false;
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
