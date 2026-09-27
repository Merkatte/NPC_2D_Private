using System;

public interface IInteractionReservation : IDisposable
{
    bool IsValid { get; }
}
