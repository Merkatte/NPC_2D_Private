public interface IHealthState
{
    float CurrentHealth { get; }
    float ChangeHealth(float amount);
}
