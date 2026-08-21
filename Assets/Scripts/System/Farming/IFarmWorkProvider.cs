public interface IFarmWorkProvider
{
    bool CanApplyWork { get; }
    bool TryApplyWork(float workerEfficiency, out FarmWorkResult result);
}
