using UnityEngine;

/// <summary>
/// The player's single global gold balance. Gold is not owned per NPC or per building: anything
/// earned is immediately spendable anywhere, so one scene-wide component owns the whole scalar.
///
/// This is the only type permitted to mutate the balance — consumers read <see cref="CurrentGold"/>
/// and go through <see cref="Add"/> / <see cref="TrySpend"/>. This mirrors the ownership discipline
/// of WorkerInventory.Clear(), which only the owning NPCComponent may call.
///
/// Reached by serialized reference like NPCManager and UIManager, deliberately not a static
/// instance — DataManager.instance is this project's one exception and is recorded as debt.
/// </summary>
public class GoldManager : MonoBehaviour
{
    [SerializeField, Min(0)] private int _initialGold;

    private int _currentGold;

    public int CurrentGold => _currentGold;

    // Unity deserializes serialized fields after field initializers run, so _currentGold cannot be
    // seeded from _initialGold at field-initializer scope — it would always read 0.
    private void Awake()
    {
        _currentGold = Mathf.Max(0, _initialGold);
    }

    /// <summary>
    /// Earns gold. Non-positive amounts are not an earn and are silently ignored, matching
    /// WarehouseInventory.TryAdd's rejection of non-positive quantities.
    /// Saturates at int.MaxValue rather than overflowing: Add has no failure channel, so refusing
    /// is not an option and wrapping into a negative balance would be worse than clamping.
    /// </summary>
    public void Add(int amount)
    {
        if (amount <= 0)
            return;

        long sum = (long)_currentGold + amount;
        _currentGold = sum > int.MaxValue ? int.MaxValue : (int)sum;
    }

    /// <summary>
    /// All-or-nothing spend; no partial spend. Returns false and leaves the balance untouched when
    /// the amount is non-positive or the player cannot afford it.
    ///
    /// Insufficient funds is a normal gameplay outcome, not a configuration error, so it is never
    /// logged here — the caller decides how to present a rejected purchase.
    /// </summary>
    public bool TrySpend(int amount)
    {
        if (amount <= 0)
            return false;

        if (_currentGold < amount)
            return false;

        _currentGold -= amount;
        return true;
    }
}
