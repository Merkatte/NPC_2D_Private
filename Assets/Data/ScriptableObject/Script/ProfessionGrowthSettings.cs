using System;
using UnityEngine;

[CreateAssetMenu(menuName = "NPC/Profession Growth Settings")]
public sealed class ProfessionGrowthSettings : ScriptableObject
{
    [Serializable]
    public sealed class ProfessionRule
    {
        [SerializeField] private NPCType _profession;
        [SerializeField] private int _baseLevelCap = 5;
        [SerializeField] private float _baseRequiredExperience = 100f;
        [SerializeField] private float _requiredExperiencePerLevel = 50f;
        [SerializeField] private float _farmProgressExperience;
        [SerializeField] private float _harvestItemExperience;
        [SerializeField] private float _constructionWorkExperience;
        [SerializeField] private float _repairedHealthExperience;
        [SerializeField] private float _maintenanceSecondExperience;
        [SerializeField] private float _damageExperience;

        public NPCType Profession => _profession;
        public int BaseLevelCap => Mathf.Max(1, _baseLevelCap);
        public float RequiredExperience(int level)
        {
            double value = PositiveOrDefault(_baseRequiredExperience, 100f) +
                (double)NonNegative(_requiredExperiencePerLevel) * Math.Max(0, level - 1);
            return (float)Math.Min(float.MaxValue, value);
        }
        public float ExperiencePerUnit(WorkContributionKind kind)
        {
            switch (kind)
            {
                case WorkContributionKind.FarmProgress: return NonNegative(_farmProgressExperience);
                case WorkContributionKind.HarvestItems: return NonNegative(_harvestItemExperience);
                case WorkContributionKind.ConstructionWork: return NonNegative(_constructionWorkExperience);
                case WorkContributionKind.RepairedHealth: return NonNegative(_repairedHealthExperience);
                case WorkContributionKind.MaintenanceSeconds: return NonNegative(_maintenanceSecondExperience);
                case WorkContributionKind.Damage: return NonNegative(_damageExperience);
                default: return 0f;
            }
        }
        public ProfessionRule(NPCType profession)
        {
            _profession = profession;
            if (profession == NPCType.Farmer) { _farmProgressExperience = 0.5f; _harvestItemExperience = 2f; }
            if (profession == NPCType.Builder)
            { _constructionWorkExperience = 0.2f; _repairedHealthExperience = 0.1f; _maintenanceSecondExperience = 2f; }
            if (profession == NPCType.Guard) _damageExperience = 0.1f;
            if (profession == NPCType.Archer) _damageExperience = 0.3f;
        }
        internal void Validate()
        {
            _baseLevelCap = BaseLevelCap;
            _baseRequiredExperience = PositiveOrDefault(_baseRequiredExperience, 100f);
            _requiredExperiencePerLevel = NonNegative(_requiredExperiencePerLevel);
            _farmProgressExperience = NonNegative(_farmProgressExperience);
            _harvestItemExperience = NonNegative(_harvestItemExperience);
            _constructionWorkExperience = NonNegative(_constructionWorkExperience);
            _repairedHealthExperience = NonNegative(_repairedHealthExperience);
            _maintenanceSecondExperience = NonNegative(_maintenanceSecondExperience);
            _damageExperience = NonNegative(_damageExperience);
        }
        private static float NonNegative(float value)
            => float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Max(0f, value);
        private static float PositiveOrDefault(float value, float fallback)
            => value > 0f && !float.IsInfinity(value) ? value : fallback;
    }

    [SerializeField] private ProfessionRule[] _professions =
    {
        new ProfessionRule(NPCType.Farmer), new ProfessionRule(NPCType.Builder),
        new ProfessionRule(NPCType.Guard), new ProfessionRule(NPCType.Archer)
    };

    public bool TryGetRule(NPCType profession, out ProfessionRule rule)
    {
        if (_professions != null)
            foreach (ProfessionRule candidate in _professions)
                if (candidate != null && candidate.Profession == profession)
                { rule = candidate; return true; }
        rule = null;
        return false;
    }
    private void OnValidate()
    {
        if (_professions == null) return;
        foreach (ProfessionRule rule in _professions) rule?.Validate();
    }
}
