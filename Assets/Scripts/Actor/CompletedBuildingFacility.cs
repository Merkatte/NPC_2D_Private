using UnityEngine;

// Serialized prefab contract. Holds cached references; contains no construction rules.
public sealed class CompletedBuildingFacility : MonoBehaviour
{
    [SerializeField] private Transform _entrance;
    [SerializeField] private BaseInteractionProvider _provider;
    [SerializeField] private WarehouseDepositPoint _warehouse;
    [SerializeField] private FarmWorkSite _farm;
    [SerializeField] private FarmSeedSource _seedSource;
    [SerializeField] private GuardPost _guardPost;
    [SerializeField] private Pub _pub;
    [SerializeField] private House _house;
    [SerializeField] private SpriteRenderer _houseVisual;
    [SerializeField] private HousePopupSource _housePopupSource;
    private DestinationDB _destinations;
    private InteractableManager _interactables;
    private DestinationInfo _registration;
    private bool _providerRegistered;
    public House House => _house;
    public HousePopupSource HousePopupSource => _housePopupSource;
    public Transform Entrance => _entrance;

    internal bool TryConfigureHouse(HousingDataContext data, HouseTierDefinition tier,
        HousingManager manager, Sprite sprite, out string reason)
    {
        reason = "House prefab requires its house, visual, popup source, entrance and tier sprite.";
        if (!_house || !_houseVisual || !_housePopupSource || !_entrance || !sprite)
            return false;
        if (!_house.TryConfigure(data, tier, manager, _entrance, out reason))
            return false;
        _houseVisual.sprite = sprite;
        return true;
    }

    internal bool TryApplyHouseTier(HouseTierDefinition tier, Sprite sprite, out string reason)
    {
        reason = "House upgrade is missing its live house, visual or sprite.";
        if (!_house || !_houseVisual || !sprite)
            return false;
        Sprite previous = _houseVisual.sprite;
        _houseVisual.sprite = sprite;
        // Domain change notifications observe the matching presentation. A rejected
        // tier leaves both the runtime and the visual at the previous value.
        if (!_house.TryApplyTier(tier, out reason))
        {
            _houseVisual.sprite = previous;
            return false;
        }
        return true;
    }
    public BaseInteractionProvider Provider => _provider;

    internal bool TryConfigure(BuildingDefinition definition, ResourceManager resources, ItemDataContext items,
        CropCatalog crops, BoxCollider2D workArea, out string reason)
    {
        reason = "Facility prefab has missing entrance or required components.";
        if (!_entrance) return false;
        switch (definition.BuildingType)
        {
            case BuildingType.Pub:
                if (!_pub || _provider != _pub) return false;
                _pub.Configure(items);
                break;
            case BuildingType.Warehouse:
                if (!_warehouse || _provider != _warehouse) return false;
                _warehouse.Configure(resources, definition.ProvidedCapacity);
                break;
            case BuildingType.Farm:
                if (!_farm || !_seedSource || _provider != _farm || !workArea) return false;
                _farm.ConfigureConstruction(workArea);
                _seedSource.Configure(resources, crops, items);
                if (!_seedSource.TryInitialize()) return false;
                break;
            case BuildingType.GuardPost:
                if (!_guardPost || _provider != _guardPost || !workArea) return false;
                _guardPost.ConfigureConstruction(workArea);
                break;
            case BuildingType.House:
                if (!_house || _house.Tier == null || _provider) return false;
                break;
            case BuildingType.Inn:
                if (_provider) return false;
                break;
            default: return false;
        }
        if (_provider && !_provider.TryInitialize(out reason)) return false;
        reason = null;
        return true;
    }

    internal bool TryRegister(BuildingType type, DestinationDB destinations, InteractableManager interactables)
    {
        if (type == BuildingType.House) return _house && _house.isActiveAndEnabled;
        _destinations = destinations;
        _interactables = interactables;
        if (_provider)
        {
            if (!interactables.TryRegisterProvider(_provider)) return false;
            _providerRegistered = true;
        }
        _registration = new DestinationInfo { BuildingType = type, DestinationLoc = _entrance,
            DestinationObject = _provider ? _provider.gameObject : gameObject };
        if (!destinations.Register(_registration) || (_warehouse && !_warehouse.RegisterCapacity()))
        { RemoveRegistrations(); return false; }
        return true;
    }

    internal void RemoveRegistrations()
    {
        if (_destinations && _registration != null) _destinations.Unregister(_registration);
        if (_interactables && _providerRegistered) _interactables.UnregisterProvider(_provider);
        if (_warehouse) _warehouse.UnregisterCapacity();
        _registration = null;
        _providerRegistered = false;
    }
    private void OnDestroy() { RemoveRegistrations(); }
}
