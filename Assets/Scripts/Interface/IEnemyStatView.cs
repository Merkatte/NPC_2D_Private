public interface IEnemyStatView : ICombatStatView
{
    AttackStyle Style { get; }

    // How deep into AttackRange a melee Enemy closes before stopping (0..1). Not used by Ranged.
    float PreferredAttackRangeRatio { get; }
}
