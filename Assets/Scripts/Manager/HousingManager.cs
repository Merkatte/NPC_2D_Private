using System.Collections.Generic;
using UnityEngine;

public sealed class HousingManager : MonoBehaviour
{
    [SerializeField] private HousingDataContext _data;
    [SerializeField] private NPCManager _npcManager;
    [SerializeField] private TilemapNavigation _navigation;
    private readonly List<House> _houses = new List<House>();
    private readonly List<ResidentHousingState> _unhoused = new List<ResidentHousingState>();
    private readonly HousingAssignmentPolicy _policy = new HousingAssignmentPolicy();
    private float _elapsed;
    private bool _isAssigning;
    public HousingDataContext Data => _data;
    public INavigationService Navigation => _navigation;
    public HousingLifeSettings LifeSettings => _data ? _data.LifeSettings : null;

    private void Awake()
    {
        string reason = "HousingManager requires data, NPCManager and navigation.";
        if (!_data || !_npcManager || !_navigation || !_data.TryInitialize(out reason))
        {
            Debug.LogError($"HousingManager '{name}': {reason}", this);
            enabled = false;
        }
    }

    public void Register(House house)
    {
        if (!house || _houses.Contains(house)) return;
        _houses.Add(house);
        house.Changed += AssignResidents;
        AssignResidents();
    }

    public void Unregister(House house)
    {
        if (!_houses.Remove(house)) return;
        house.Changed -= AssignResidents;
        while (house.Residents.Count > 0) house.RemoveResident(house.Residents[house.Residents.Count - 1]);
        AssignResidents();
    }

    private void OnEnable()
    {
        if (_npcManager) _npcManager.ResidentsChanged += AssignResidents;
    }

    private void OnDisable()
    {
        if (_npcManager) _npcManager.ResidentsChanged -= AssignResidents;
    }

    private void Update()
    {
        if (LifeSettings == null) return;
        _elapsed += Time.deltaTime;
        if (_elapsed < LifeSettings.ReassessmentSeconds) return;
        _elapsed = 0f;
        AssignResidents();
    }

    private void AssignResidents()
    {
        if (_isAssigning || !isActiveAndEnabled || _houses.Count == 0 || !_npcManager || LifeSettings == null || !_navigation || !_navigation.IsReady) return;
        _isAssigning = true;
        try
        {
            _unhoused.Clear();
            foreach (ResidentHousingState resident in _npcManager.Residents)
                if (resident.IsRegistered && !resident.Home) _unhoused.Add(resident);
            _unhoused.Sort(HousingAssignmentPolicy.CompareResidents);
            foreach (ResidentHousingState resident in _unhoused)
            {
                House home = _policy.SelectNearest(resident, _houses, _navigation);
                if (home) home.TryAddResident(resident);
            }
        }
        finally { _unhoused.Clear(); _isAssigning = false; }
    }
}
