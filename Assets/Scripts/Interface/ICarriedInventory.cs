public interface ICarriedInventory : IInventory
{
    bool IsEmpty { get; }
    bool IsFull { get; }

    /// <summary>
    /// Moves the carried stack into <paramref name="destination"/>. Quantity the destination
    /// refuses stays carried, so a rejected or partial deposit never destroys produce.
    /// Returns true when at least one unit moved.
    /// </summary>
    bool TryTransferAllTo(IInventory destination, out int movedQuantity);
}
